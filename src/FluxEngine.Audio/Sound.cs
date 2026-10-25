namespace FluxEngine.Audio;

// decoded audio kept as floats and ready to mix
public sealed class Sound
{
    internal float[] Samples { get; }
    internal int Frames { get; }
    internal int Channels { get; }

    public int SampleRate { get; }

    public float Duration => SampleRate <= 0 ? 0 : Frames / (float)SampleRate;

    internal Sound(float[] samples, int channels, int sampleRate)
    {
        Samples = samples;
        Channels = channels;
        SampleRate = sampleRate;
        Frames = channels <= 0 ? 0 : samples.Length / channels;
    }
}
