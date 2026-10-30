using System.Text.Json;

namespace FluxEngine.Core;

public sealed class EngineSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private int _targetFps = 60;
    private float _masterVolume = 1f;
    private float _sfxVolume = 1f;
    private float _musicVolume = 0.8f;

    public bool EnableLighting { get; set; } = true;
    public bool EnableVignette { get; set; } = true;
    public bool EnableGodRays { get; set; } = true;
    public bool EnableAtmosphericParticles { get; set; } = true;
    public bool EnableFog { get; set; } = true;
    public bool EnableScreenShake { get; set; } = true;

    // Display & Performance
    public bool Vsync { get; set; } = true;
    public int TargetFps
    {
        get => _targetFps;
        set => _targetFps = Math.Clamp(value, 0, 1000);
    }

    // Audio
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

    public event Action<EngineSettings>? Changed;

    public void NotifyChanged()
    {
        Changed?.Invoke(this);
    }

    private static float NormalizeVolume(float value)
        => float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : 0f;

    public static EngineSettings Load(string path = "settings.json")
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<EngineSettings>(json, JsonOptions);
                if (loaded is not null)
                    return loaded;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load settings from '{path}': {ex.Message}. Using defaults.");
        }

        return new EngineSettings();
    }

    public bool Save(string path = "settings.json")
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save settings to '{path}': {ex.Message}");
            return false;
        }
    }
}
