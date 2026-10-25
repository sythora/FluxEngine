using FluxEngine.Graphics;

namespace FluxEngine.Content;

// caches textures by full path so asking for the same file twice is free
public sealed class AssetManager
{
    private readonly Dictionary<string, Texture> _textures = [];

    public int TextureCount => _textures.Count;

    public Texture LoadTexture(Renderer renderer, string path)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);

        if (_textures.TryGetValue(fullPath, out var cached))
            return cached;

        var texture = renderer.LoadTexture(fullPath);
        _textures[fullPath] = texture;
        return texture;
    }

    // kills the texture, so stop drawing with it first
    public void UnloadTexture(Texture texture)
    {
        string? key = null;

        foreach (var pair in _textures)
        {
            if (pair.Value == texture)
            {
                key = pair.Key;
                break;
            }
        }

        if (key is not null)
            _textures.Remove(key);

        texture.Dispose();
    }

    public void Clear()
    {
        foreach (var texture in _textures.Values)
            texture.Dispose();

        _textures.Clear();
    }
}
