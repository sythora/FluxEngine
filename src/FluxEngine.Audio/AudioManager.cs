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

// One device and callback; active sounds are mixed together.
public sealed class AudioManager : IDisposable
{
    private readonly List<Playback> _playing = [];
    private readonly Dictionary<string, Sound> _sounds = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly object _lock = new();
    private readonly SDL.SDL_AudioCallback _callback;

    private SDL.SDL_AudioSpec _spec;
    private float[] _mixBuffer = [];
    private byte[] _writeBuffer = [];
    private uint _device;
    private bool _audioSubsystemInitialized;
    private bool _disposed;
    private float _masterVolume = 1f;
    private float _sfxVolume = 1f;
    private float _musicVolume = 1f;

    public AudioManager()
    {
        _callback = Mix;

        try
        {
            _audioSubsystemInitialized = SDL.SDL_InitSubSystem(SDL.SDL_INIT_AUDIO) == 0;
            if (!_audioSubsystemInitialized)
                Log.Warn($"Audio subsystem didn't start: {SDL.SDL_GetError()}");
        }
        catch (DllNotFoundException exception)
        {
            Log.Warn($"Audio subsystem is unavailable: {exception.Message}");
        }
        catch (EntryPointNotFoundException exception)
        {
            Log.Warn($"Audio subsystem is unavailable: {exception.Message}");
        }
    }

    public bool IsAvailable => _device != 0;
    public bool IsPaused { get; private set; }
    public int ActiveVoiceCount
    {
        get
        {
            lock (_lock)
                return _playing.Count;
        }
    }

    public int SoundCount => _sounds.Count;
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

        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);

        if (_sounds.TryGetValue(fullPath, out var cached))
            return cached;

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"No wav at '{fullPath}'.", fullPath);

        var (spec, samples) = ReadWave(fullPath);
        EnsureDevice(spec);
        var channels = spec.channels;
        var sampleRate = spec.freq;

        if (_device != 0)
        {
            samples = ConvertSamples(samples, spec.channels, spec.freq, _spec.channels, _spec.freq);
            channels = _spec.channels;
            sampleRate = _spec.freq;
        }

        var sound = new Sound(samples, channels, sampleRate);
        _sounds.Add(fullPath, sound);
        return sound;
    }

    // volume 0..1, loop keeps it going forever
    public void Play(Sound sound, float volume = 1f, bool loop = false, AudioBus bus = AudioBus.Sfx)
    {
        ArgumentNullException.ThrowIfNull(sound);

        ObjectDisposedException.ThrowIf(_disposed, this);

        if (bus is not AudioBus.Sfx and not AudioBus.Music)
            throw new ArgumentOutOfRangeException(nameof(bus));

        if (_device == 0)
            return;

        volume = NormalizeVolume(volume);

        if (volume <= 0)
            return;

        lock (_lock)
            _playing.Add(new Playback(sound, volume, loop, bus));
    }

    public bool UnloadSound(Sound sound)
    {
        ArgumentNullException.ThrowIfNull(sound);

        string? path = null;
        foreach (var pair in _sounds)
        {
            if (ReferenceEquals(pair.Value, sound))
            {
                path = pair.Key;
                break;
            }
        }

        return path is not null && _sounds.Remove(path);
    }

    public void ClearSounds() => _sounds.Clear();

    public void StopAll()
    {
        lock (_lock)
            _playing.Clear();
    }

    public void SetPaused(bool paused)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        IsPaused = paused;

        if (_device == 0)
            return;

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

        if (_audioSubsystemInitialized)
            SDL.SDL_QuitSubSystem(SDL.SDL_INIT_AUDIO);
    }

    private void EnsureDevice(SDL.SDL_AudioSpec source)
    {
        if (_device != 0 || !_audioSubsystemInitialized)
            return;

        var wanted = new SDL.SDL_AudioSpec
        {
            freq = source.freq,
            format = source.format,
            channels = source.channels,
            samples = 2048,
            callback = _callback,
        };

        try
        {
            _device = SDL.SDL_OpenAudioDevice(null!, 0, ref wanted, out _spec, 0);
        }
        catch (DllNotFoundException exception)
        {
            Log.Warn($"No audio device, everything stays quiet: {exception.Message}");
            return;
        }
        catch (EntryPointNotFoundException exception)
        {
            Log.Warn($"No audio device, everything stays quiet: {exception.Message}");
            return;
        }

        if (_device == 0)
        {
            Log.Warn($"No audio device, everything stays quiet: {SDL.SDL_GetError()}");
            return;
        }

        SDL.SDL_PauseAudioDevice(_device, IsPaused ? 1 : 0);
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

        lock (_lock)
        {
            for (var voiceIndex = _playing.Count - 1; voiceIndex >= 0; voiceIndex--)
            {
                var playback = _playing[voiceIndex];
                var mixedFrames = 0;

                while (mixedFrames < frameCount)
                {
                    var available = playback.Frames - playback.Frame;

                    if (available <= 0)
                    {
                        if (!playback.Loop)
                            break;

                        playback.Frame = 0;
                        available = playback.Frames;
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
                }

                if (!playback.Loop && playback.Frame >= playback.Frames)
                    _playing.RemoveAt(voiceIndex);
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

    private static float[] ConvertSamples(float[] samples, int sourceChannels, int sourceRate, int targetChannels, int targetRate)
    {
        if (sourceChannels <= 0 || sourceRate <= 0 || targetChannels <= 0 || targetRate <= 0)
            throw new InvalidOperationException("The WAV file has an invalid sample rate or channel count.");

        var sourceFrames = samples.Length / sourceChannels;
        if (sourceFrames == 0)
            throw new InvalidOperationException("The WAV file contains no complete audio frames.");

        var targetFrameCount = Math.Max(1d, Math.Round(sourceFrames * (double)targetRate / sourceRate));
        if (targetFrameCount > int.MaxValue / targetChannels)
            throw new InvalidOperationException("The converted WAV file is too large to load.");

        var targetFrames = (int)targetFrameCount;
        var converted = new float[targetFrames * targetChannels];

        for (var frame = 0; frame < targetFrames; frame++)
        {
            var sourcePosition = frame * (double)sourceRate / targetRate;
            var firstFrame = Math.Min((int)sourcePosition, sourceFrames - 1);
            var secondFrame = Math.Min(firstFrame + 1, sourceFrames - 1);
            var blend = (float)(sourcePosition - firstFrame);

            for (var channel = 0; channel < targetChannels; channel++)
            {
                float first;
                float second;

                if (targetChannels == 1 && sourceChannels > 1)
                {
                    first = AverageChannels(samples, firstFrame, sourceChannels);
                    second = AverageChannels(samples, secondFrame, sourceChannels);
                }
                else
                {
                    var sourceChannel = sourceChannels == 1 ? 0 : Math.Min(channel, sourceChannels - 1);
                    first = samples[firstFrame * sourceChannels + sourceChannel];
                    second = samples[secondFrame * sourceChannels + sourceChannel];
                }

                converted[frame * targetChannels + channel] = first + (second - first) * blend;
            }
        }

        return converted;
    }

    private static float AverageChannels(float[] samples, int frame, int channels)
    {
        var total = 0f;
        var offset = frame * channels;
        for (var channel = 0; channel < channels; channel++)
            total += samples[offset + channel];

        return total / channels;
    }

    private static (SDL.SDL_AudioSpec Spec, float[] Samples) ReadWave(string path)
    {
        var wave = File.ReadAllBytes(path);

        if (wave.Length < 12
            || !wave.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            || !wave.AsSpan(8, 4).SequenceEqual("WAVE"u8))
            throw new InvalidDataException($"'{path}' is not a RIFF/WAVE file.");

        var formatOffset = -1;
        var formatLength = 0;
        var dataOffset = -1;
        var dataLength = 0;
        var chunkOffset = 12;

        while (chunkOffset <= wave.Length - 8)
        {
            var chunkLengthValue = BinaryPrimitives.ReadUInt32LittleEndian(wave.AsSpan(chunkOffset + 4, 4));
            if (chunkLengthValue > int.MaxValue)
                throw new InvalidDataException($"'{path}' contains an oversized WAV chunk.");

            var chunkLength = (int)chunkLengthValue;
            var chunkDataOffset = chunkOffset + 8;
            if (chunkLength > wave.Length - chunkDataOffset)
                throw new InvalidDataException($"'{path}' contains a truncated WAV chunk.");

            if (wave.AsSpan(chunkOffset, 4).SequenceEqual("fmt "u8))
            {
                formatOffset = chunkDataOffset;
                formatLength = chunkLength;
            }
            else if (wave.AsSpan(chunkOffset, 4).SequenceEqual("data"u8))
            {
                dataOffset = chunkDataOffset;
                dataLength = chunkLength;
            }

            chunkOffset = chunkDataOffset + chunkLength + (chunkLength & 1);
        }

        if (formatOffset < 0 || formatLength < 16 || dataOffset < 0 || dataLength == 0)
            throw new InvalidDataException($"'{path}' is missing a valid WAV format or audio data chunk.");

        var encoding = BinaryPrimitives.ReadUInt16LittleEndian(wave.AsSpan(formatOffset, 2));
        if (encoding == 0xfffe && formatLength >= 40)
            encoding = BinaryPrimitives.ReadUInt16LittleEndian(wave.AsSpan(formatOffset + 24, 2));

        var channels = BinaryPrimitives.ReadUInt16LittleEndian(wave.AsSpan(formatOffset + 2, 2));
        var sampleRateValue = BinaryPrimitives.ReadUInt32LittleEndian(wave.AsSpan(formatOffset + 4, 4));
        var bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(wave.AsSpan(formatOffset + 14, 2));

        if (channels == 0 || channels > byte.MaxValue || sampleRateValue == 0 || sampleRateValue > int.MaxValue)
            throw new InvalidDataException($"'{path}' has an invalid sample rate or channel count.");

        var sampleFormat = (encoding, bitsPerSample) switch
        {
            (1, 8) => SDL.AUDIO_U8,
            (1, 16) => SDL.AUDIO_S16LSB,
            (1, 32) => SDL.AUDIO_S32LSB,
            (3, 32) => SDL.AUDIO_F32LSB,
            _ => (ushort)0,
        };

        if (sampleFormat == 0)
            throw new InvalidDataException($"'{path}' uses an unsupported WAV encoding. Use 8/16/32-bit PCM or 32-bit float.");

        var sampleBytes = BytesPerSample(sampleFormat);
        if (dataLength % sampleBytes != 0 || dataLength / sampleBytes % channels != 0)
            throw new InvalidDataException($"'{path}' has incomplete WAV sample frames.");

        var samples = Decode(wave.AsSpan(dataOffset, dataLength).ToArray(), sampleFormat)
            ?? throw new InvalidDataException($"'{path}' uses an unsupported WAV encoding.");

        var spec = new SDL.SDL_AudioSpec
        {
            freq = (int)sampleRateValue,
            format = sampleFormat,
            channels = (byte)channels,
            samples = 2048,
        };
        return (spec, samples);
    }

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

    // turns supported WAV sample formats into floats
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
                SDL.AUDIO_F32LSB => ReadFloat(raw, offset, true),
                SDL.AUDIO_F32MSB => ReadFloat(raw, offset, false),
                _ => 0f,
            };

            if (!float.IsFinite(samples[i]))
                samples[i] = 0f;
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

    private static float ReadFloat(byte[] data, int offset, bool littleEndian)
        => BitConverter.Int32BitsToSingle(Read32(data, offset, littleEndian));

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
