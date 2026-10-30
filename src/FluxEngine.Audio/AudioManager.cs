using System.Buffers.Binary;
using System.Runtime.InteropServices;
using SDL2;
using FluxEngine.Core;

namespace FluxEngine.Audio;

public enum AudioBus
{
    Sfx,
    Music,
}

// One device and callback; sounds queue instead of playing simultaneously.
public sealed class AudioManager : IDisposable
{
    private readonly List<Playback> _playing = [];
    private readonly object _lock = new();
    private readonly SDL.SDL_AudioCallback _callback;

    private SDL.SDL_AudioSpec _spec;
    private float[] _mixBuffer = [];
    private byte[] _writeBuffer = [];
    private uint _device;
    private bool _disposed;
    private float _masterVolume = 1f;
    private float _sfxVolume = 1f;
    private float _musicVolume = 1f;

    public AudioManager()
    {
        _callback = Mix;

        if (SDL.SDL_InitSubSystem(SDL.SDL_INIT_AUDIO) != 0)
            Log.Warn($"Audio subsystem didn't start: {SDL.SDL_GetError()}");
    }

    public bool IsAvailable => _device != 0;
    public bool IsPaused { get; private set; }
    public float MasterVolume
    {
        get => _masterVolume;
        set => _masterVolume = NormalizeVolume(value);
    }

    public float SfxVolume
    {
        get => _sfxVolume;
        set => _sfxVolume = NormalizeVolume(value);
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set => _musicVolume = NormalizeVolume(value);
    }

    // device stays closed until the first sound loads, so it can match the
    // file format exactly and skip converting that one
    public Sound LoadSound(string path)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AudioManager));

        if (!File.Exists(path))
            throw new FileNotFoundException($"No wav at '{path}'.", path);

        var source = new SDL.SDL_AudioSpec();
        var buffer = IntPtr.Zero;
        uint byteLength = 0;
        SDL.SDL_AudioSpec spec;

        try
        {
            spec = SDL.SDL_LoadWAV(path, ref source, out buffer, out byteLength);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Couldn't read wav '{path}': {SDL.SDL_GetError()}", exception);
        }

        try
        {
            if (buffer == IntPtr.Zero || byteLength == 0)
                throw new InvalidOperationException($"Couldn't load wav '{path}': {SDL.SDL_GetError()}");

            var raw = new byte[byteLength];
            Marshal.Copy(buffer, raw, 0, raw.Length);

            var samples = Decode(raw, spec.format);

            if (samples is null)
                throw new InvalidOperationException($"'{path}' uses an audio format I don't handle, use 8 or 16 bit pcm or f32.");

            EnsureDevice(spec);
            return new Sound(samples, spec.channels, spec.freq);
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                SDL.SDL_FreeWAV(buffer);
        }
    }

    // volume 0..1, loop keeps it going forever
    public void Play(Sound sound, float volume = 1f, bool loop = false, AudioBus bus = AudioBus.Sfx)
    {
        ArgumentNullException.ThrowIfNull(sound);

        if (_device == 0)
            return;

        volume = NormalizeVolume(volume);

        if (volume <= 0)
            return;

        lock (_lock)
            _playing.Add(new Playback(sound, volume, loop, bus));
    }

    public void StopAll()
    {
        lock (_lock)
            _playing.Clear();
    }

    public void SetPaused(bool paused)
    {
        if (_device == 0)
            return;

        IsPaused = paused;
        SDL.SDL_PauseAudioDevice(_device, paused ? 1 : 0);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        StopAll();

        if (_device != 0)
        {
            SDL.SDL_CloseAudioDevice(_device);
            _device = 0;
        }

        SDL.SDL_QuitSubSystem(SDL.SDL_INIT_AUDIO);
    }

    private void EnsureDevice(SDL.SDL_AudioSpec source)
    {
        if (_device != 0)
            return;

        var wanted = new SDL.SDL_AudioSpec
        {
            freq = source.freq,
            format = source.format,
            channels = source.channels,
            samples = 2048,
            callback = _callback,
        };

        _device = SDL.SDL_OpenAudioDevice(null!, 0, ref wanted, out _spec, 0);

        if (_device == 0)
        {
            Log.Warn($"No audio device, everything stays quiet: {SDL.SDL_GetError()}");
            return;
        }

        SDL.SDL_PauseAudioDevice(_device, 0);
    }

    // called by SDL on its audio thread, keep it boring and quick
    private void Mix(IntPtr userdata, IntPtr stream, int length)
    {
        if (length <= 0 || _spec.channels == 0)
            return;

        var channels = _spec.channels;
        var bytesPerSample = BytesPerSample(_spec.format);
        var frameCount = length / (channels * bytesPerSample);

        if (frameCount <= 0)
            return;

        var sampleCount = frameCount * channels;
        EnsureBuffers(sampleCount, length);
        Array.Clear(_mixBuffer, 0, sampleCount);

        var mixedFrames = 0;

        lock (_lock)
        {
            while (mixedFrames < frameCount && _playing.Count > 0)
            {
                var playback = _playing[0];
                var available = playback.Frames - playback.Frame;

                if (available <= 0)
                {
                    _playing.RemoveAt(0);
                    continue;
                }

                var frames = Math.Min(frameCount - mixedFrames, available);
                var sourceIndex = playback.Frame * channels;
                var targetIndex = mixedFrames * channels;
                var busVolume = playback.Bus == AudioBus.Music ? MusicVolume : SfxVolume;
                var volume = playback.Volume * MasterVolume * busVolume;

                for (var i = 0; i < frames * channels; i++)
                    _mixBuffer[targetIndex + i] += playback.Samples[sourceIndex + i] * volume;

                playback.Frame += frames;
                mixedFrames += frames;

                if (playback.Frame >= playback.Frames)
                {
                    if (playback.Loop)
                        playback.Frame = 0;
                    else
                        _playing.RemoveAt(0);
                }
            }
        }

        WriteSamples(stream, sampleCount, bytesPerSample);
    }

    private void EnsureBuffers(int sampleCount, int byteCount)
    {
        if (_mixBuffer.Length < sampleCount)
            _mixBuffer = new float[sampleCount];

        if (_writeBuffer.Length < byteCount)
            _writeBuffer = new byte[byteCount];
    }

    private static float NormalizeVolume(float value)
        => float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : 0f;

    private void WriteSamples(IntPtr stream, int sampleCount, int bytesPerSample)
    {
        var format = _spec.format;
        var output = _writeBuffer;

        for (var i = 0; i < sampleCount; i++)
        {
            var value = Math.Clamp(_mixBuffer[i], -1f, 1f);
            var offset = i * bytesPerSample;

            switch (format)
            {
                case SDL.AUDIO_U8:
                    output[offset] = (byte)((value * 0.5f + 0.5f) * 255);
                    break;

                case SDL.AUDIO_S8:
                    output[offset] = (byte)(sbyte)(value * 127);
                    break;

                case SDL.AUDIO_S16LSB:
                    Write16(output, offset, (short)(value * 32767), true);
                    break;

                case SDL.AUDIO_S16MSB:
                    Write16(output, offset, (short)(value * 32767), false);
                    break;

                case SDL.AUDIO_S32LSB:
                    Write32(output, offset, (int)(value * 2147483647), true);
                    break;

                case SDL.AUDIO_S32MSB:
                    Write32(output, offset, (int)(value * 2147483647), false);
                    break;

                case SDL.AUDIO_F32LSB:
                    Write32(output, offset, BitConverter.SingleToInt32Bits(value), true);
                    break;

                default:
                    Write32(output, offset, BitConverter.SingleToInt32Bits(value), false);
                    break;
            }
        }

        Marshal.Copy(output, 0, stream, sampleCount * bytesPerSample);
    }

    // turns whatever SDL loaded into floats. returns null for formats we skip
    private static float[]? Decode(byte[] raw, ushort format)
    {
        var bytesPerSample = BytesPerSample(format);

        if (bytesPerSample == 0)
            return null;

        var count = raw.Length / bytesPerSample;
        var samples = new float[count];

        for (var i = 0; i < count; i++)
        {
            var offset = i * bytesPerSample;

            samples[i] = format switch
            {
                SDL.AUDIO_U8 => (raw[offset] - 128) / 128f,
                SDL.AUDIO_S8 => (sbyte)raw[offset] / 128f,
                SDL.AUDIO_S16LSB => Read16(raw, offset, true) / 32768f,
                SDL.AUDIO_S16MSB => Read16(raw, offset, false) / 32768f,
                SDL.AUDIO_S32LSB => Read32(raw, offset, true) / 2147483648f,
                SDL.AUDIO_S32MSB => Read32(raw, offset, false) / 2147483648f,
                SDL.AUDIO_F32LSB => BitConverter.Int32BitsToSingle(Read32(raw, offset, true)),
                SDL.AUDIO_F32MSB => BitConverter.Int32BitsToSingle(Read32(raw, offset, false)),
                _ => 0f,
            };
        }

        return samples;
    }

    private static int BytesPerSample(ushort format) => format switch
    {
        SDL.AUDIO_U8 or SDL.AUDIO_S8 => 1,
        SDL.AUDIO_S16LSB or SDL.AUDIO_S16MSB => 2,
        SDL.AUDIO_S32LSB or SDL.AUDIO_S32MSB or SDL.AUDIO_F32LSB or SDL.AUDIO_F32MSB => 4,
        _ => 0,
    };

    private static short Read16(byte[] data, int offset, bool littleEndian)
        => littleEndian
            ? BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset, 2))
            : BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));

    private static int Read32(byte[] data, int offset, bool littleEndian)
        => littleEndian
            ? BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, 4))
            : BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset, 4));

    private static void Write16(byte[] data, int offset, short value, bool littleEndian)
    {
        if (littleEndian)
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset, 2), value);
        else
            BinaryPrimitives.WriteInt16BigEndian(data.AsSpan(offset, 2), value);
    }

    private static void Write32(byte[] data, int offset, int value, bool littleEndian)
    {
        if (littleEndian)
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset, 4), value);
        else
            BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(offset, 4), value);
    }

    private sealed class Playback(Sound sound, float volume, bool loop, AudioBus bus)
    {
        public float[] Samples { get; } = sound.Samples;
        public int Frames { get; } = sound.Frames;
        public float Volume { get; } = volume;
        public bool Loop { get; } = loop;
        public AudioBus Bus { get; } = bus;
        public int Frame;
    }
}
