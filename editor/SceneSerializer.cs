using FluxEngine.Core;
using System.Text.Json;
using FluxEngine.Mathematics;

// editor serializer facade over the engine's shared scene file format
public static class EditorSceneSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new Vector2JsonConverter() },
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
