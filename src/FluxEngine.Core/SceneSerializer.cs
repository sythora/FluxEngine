using FluxEngine.Mathematics;
using System.Text.Json;
using System.Text.Json.Serialization;

// loads and saves .scene files
public static class SceneSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new Vector2JsonConverter() },
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static SceneFile Load(string path)
    {
        if (!File.Exists(path))
            return new SceneFile();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SceneFile>(json, Options) ?? new SceneFile();
    }

    public static void Save(string path, SceneFile scene)
    {
        var json = JsonSerializer.Serialize(scene, Options);
        File.WriteAllText(path, json);
    }
}
