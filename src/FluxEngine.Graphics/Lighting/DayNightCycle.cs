using FluxEngine.Mathematics;

namespace FluxEngine.Graphics.Lighting;

public enum DayNightPhase
{
    Night,
    Dawn,
    Day,
    Dusk,
}

public readonly record struct DayNightKeyframe(float Hour, Color AmbientColor);

public sealed class DayNightCycle
{
    private readonly List<DayNightKeyframe> _keyframes = [];

    public DayNightCycle(float initialHour = 12f, float dayDuration = 120f)
    {
        DayDuration = MathF.Max(0.1f, dayDuration);
        TimeOfDay = NormalizeHour(initialHour);
        ResetDefaultKeyframes();
    }

    /// <summary>
    /// Current time of day in hours [0.0, 24.0).
    /// 0 = Midnight, 6 = Dawn, 12 = Noon, 18 = Dusk.
    /// </summary>
    public float TimeOfDay { get; private set; }

    /// <summary>
    /// Duration of a full 24-hour day cycle in real-world seconds.
    /// </summary>
    public float DayDuration { get; set; }

    /// <summary>
    /// Speed multiplier for the cycle. Set to 0 or use <see cref="IsPaused"/> to stop time.
    /// </summary>
    public float TimeScale { get; set; } = 1.0f;

    /// <summary>
    /// When true, time does not advance automatically during updates.
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// Normalized time of day in the range [0.0, 1.0).
    /// </summary>
    public float NormalizedTime => TimeOfDay / 24f;

    /// <summary>
    /// Formatted 24-hour time string (e.g. "14:30").
    /// </summary>
    public string FormattedTime
    {
        get
        {
            var totalMinutes = (int)(NormalizedTime * 24f * 60f);
            var hours = (totalMinutes / 60) % 24;
            var minutes = totalMinutes % 60;
            return $"{hours:D2}:{minutes:D2}";
        }
    }

    /// <summary>
    /// Current daylight phase.
    /// </summary>
    public DayNightPhase Phase => TimeOfDay switch
    {
        >= 5.0f and < 7.5f => DayNightPhase.Dawn,
        >= 7.5f and < 18.0f => DayNightPhase.Day,
        >= 18.0f and < 20.5f => DayNightPhase.Dusk,
        _ => DayNightPhase.Night,
    };

    public bool IsDay => Phase == DayNightPhase.Day;
    public bool IsNight => Phase == DayNightPhase.Night;

    public IReadOnlyList<DayNightKeyframe> Keyframes => _keyframes;

    public Color AmbientColor => EvaluateAmbientColor(TimeOfDay);

    public void SetHour(float hour)
    {
        TimeOfDay = NormalizeHour(hour);
    }

    public void AddHours(float hours)
    {
        SetHour(TimeOfDay + hours);
    }

    public void Update(float deltaTime)
    {
        if (IsPaused || DayDuration <= 0f || TimeScale == 0f)
            return;

        var hoursPerSecond = 24f / DayDuration;
        AddHours(deltaTime * TimeScale * hoursPerSecond);
    }

    public void ResetDefaultKeyframes()
    {
        _keyframes.Clear();
        // Midnight deep indigo
        _keyframes.Add(new DayNightKeyframe(0.0f, new Color(25, 30, 60)));
        // Pre-dawn cool twilight
        _keyframes.Add(new DayNightKeyframe(4.5f, new Color(35, 40, 80)));
        // Dawn glowing golden-rose
        _keyframes.Add(new DayNightKeyframe(6.0f, new Color(245, 145, 110)));
        // Morning clear bright sun
        _keyframes.Add(new DayNightKeyframe(7.5f, new Color(255, 235, 210)));
        // Noon full daylight
        _keyframes.Add(new DayNightKeyframe(12.0f, new Color(255, 255, 255)));
        // Golden hour late afternoon
        _keyframes.Add(new DayNightKeyframe(17.5f, new Color(255, 215, 165)));
        // Fiery sunset
        _keyframes.Add(new DayNightKeyframe(19.0f, new Color(245, 120, 75)));
        // Twilight violet dusk
        _keyframes.Add(new DayNightKeyframe(20.5f, new Color(85, 65, 125)));
        // Nightfall
        _keyframes.Add(new DayNightKeyframe(22.0f, new Color(30, 35, 70)));
        // Midnight wrap
        _keyframes.Add(new DayNightKeyframe(24.0f, new Color(25, 30, 60)));
    }

    public void SetKeyframes(IEnumerable<DayNightKeyframe> keyframes)
    {
        ArgumentNullException.ThrowIfNull(keyframes);
        var list = keyframes.OrderBy(k => k.Hour).ToList();
        if (list.Count == 0)
            throw new ArgumentException("At least one keyframe is required.", nameof(keyframes));

        _keyframes.Clear();
        _keyframes.AddRange(list);
    }

    public Color EvaluateAmbientColor(float hour)
    {
        var h = NormalizeHour(hour);

        if (_keyframes.Count == 0)
            return Color.White;
        if (_keyframes.Count == 1)
            return _keyframes[0].AmbientColor;

        // If before the first keyframe or after the last, interpolate wrap-around
        var first = _keyframes[0];
        var last = _keyframes[^1];

        for (var i = 0; i < _keyframes.Count - 1; i++)
        {
            var k0 = _keyframes[i];
            var k1 = _keyframes[i + 1];

            if (h >= k0.Hour && h <= k1.Hour)
            {
                var duration = k1.Hour - k0.Hour;
                var t = duration <= 0f ? 0f : (h - k0.Hour) / duration;
                return Color.Lerp(k0.AmbientColor, k1.AmbientColor, t);
            }
        }

        // Between last keyframe and 24.0 or before first
        if (h > last.Hour)
        {
            var span = (24f - last.Hour) + first.Hour;
            var t = span <= 0f ? 0f : (h - last.Hour) / span;
            return Color.Lerp(last.AmbientColor, first.AmbientColor, t);
        }

        if (h < first.Hour)
        {
            var span = (24f - last.Hour) + first.Hour;
            var t = span <= 0f ? 0f : ((24f - last.Hour) + h) / span;
            return Color.Lerp(last.AmbientColor, first.AmbientColor, t);
        }

        return last.AmbientColor;
    }

    private static float NormalizeHour(float hour)
    {
        var mod = hour % 24f;
        if (mod < 0f)
            mod += 24f;
        return mod;
    }
}
