using System.Text.Json;

namespace FluxEngine.Core;

public sealed class EngineSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    // Graphics Settings ("GTA 6 Mode")
    public bool EnableLighting { get; set; } = true;
    public bool EnableVignette { get; set; } = true;
    public bool EnableGodRays { get; set; } = true;
    public bool EnableAtmosphericParticles { get; set; } = true;
    public bool EnableFog { get; set; } = true;
    public bool EnableScreenShake { get; set; } = true;

    // Display & Performance
    public bool Vsync { get; set; } = true;
    public int TargetFps { get; set; } = 60;

    // Audio
    public float MasterVolume { get; set; } = 1.0f;
    public float SfxVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.8f;

    public event Action<EngineSettings>? Changed;

    public void NotifyChanged()
    {
        Changed?.Invoke(this);
    }

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
