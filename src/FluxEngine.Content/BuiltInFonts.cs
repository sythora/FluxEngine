using System.Reflection;
using System.Text;
using FluxEngine.Graphics;

namespace FluxEngine.Content;

public enum BuiltInFont
{
    // Press Start 2P by CodeMan38, SIL Open Font License 1.1.
    PressStart2P,

    // Silkscreen by Jason Kottke, SIL Open Font License 1.1.
    Silkscreen,
}

// Fonts shipped inside FluxEngine.dll. Both are open licensed (OFL 1.1), so
// games can use them without downloading or worrying about redistribution.
public static class BuiltInFonts
{
    private static readonly Assembly EngineAssembly = typeof(BuiltInFonts).Assembly;

    public static BitmapFont Load(Renderer renderer, BuiltInFont font)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        var data = BmFont.Parse(ReadFontMap(font));
        var atlasBytes = ReadResource(AtlasName(font));

        return new BitmapFont(
            data.Name,
            renderer.LoadTextureFromMemory(atlasBytes, AtlasName(font)),
            data.LineHeight,
            data.BaseLine,
            data.Glyphs);
    }

    // the raw BMFont text map, handy for inspecting metrics or tests
    public static string ReadFontMap(BuiltInFont font)
        => Encoding.UTF8.GetString(ReadResource(MapName(font)));

    public static string Name(BuiltInFont font) => font switch
    {
        BuiltInFont.PressStart2P => "Press Start 2P",
        BuiltInFont.Silkscreen => "Silkscreen",
        _ => throw new ArgumentOutOfRangeException(nameof(font)),
    };

    private static string MapName(BuiltInFont font) => font switch
    {
        BuiltInFont.PressStart2P => "PressStart2P-8.fnt",
        BuiltInFont.Silkscreen => "Silkscreen-8.fnt",
        _ => throw new ArgumentOutOfRangeException(nameof(font)),
    };

    private static string AtlasName(BuiltInFont font) => font switch
    {
        BuiltInFont.PressStart2P => "PressStart2P-8.png",
        BuiltInFont.Silkscreen => "Silkscreen-8.png",
        _ => throw new ArgumentOutOfRangeException(nameof(font)),
    };

    private static byte[] ReadResource(string fileName)
    {
        var resourceName = EngineAssembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
            throw new InvalidOperationException($"Built-in font resource '{fileName}' is missing.");

        using var stream = EngineAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Built-in font resource '{fileName}' could not be opened.");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
