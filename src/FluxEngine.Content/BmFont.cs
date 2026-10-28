using System.Globalization;
using FluxEngine.Graphics;
using FluxEngine.Mathematics;

namespace FluxEngine.Content;

// Parsed BMFont text-map data, separate from the texture so maps can be
// validated and tested without creating a renderer.
public sealed class BmFontData
{
    public string Name { get; init; } = "font";
    public string AtlasFile { get; init; } = "";
    public float LineHeight { get; init; } = 16;
    public float BaseLine { get; init; }
    public Dictionary<char, FontGlyph> Glyphs { get; } = [];
}

// Loads BMFont .fnt text maps. The format is plain text, easy to export from
// tools such as BMFont, Hiero, or Littera:
//
//   info face="My Font"
//   common lineHeight=8 base=8
//   page id=0 file="MyFont.png"
//   char id=65 x=64 y=8 width=8 height=8 xoffset=0 yoffset=0 xadvance=8
public static class BmFont
{
    public static BmFontData Parse(string mapText)
    {
        ArgumentNullException.ThrowIfNull(mapText);
        mapText = mapText.TrimStart('\uFEFF');

        var data = new BmFontData();
        var name = "";
        var atlasFile = "";
        var lineHeight = 0f;
        var baseLine = 0f;

        foreach (var rawLine in mapText.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0)
                continue;

            var tagEnd = line.IndexOf(' ');
            var tag = tagEnd < 0 ? line : line[..tagEnd];
            var fields = ParseFields(line);

            switch (tag)
            {
                case "info":
                    if (fields.TryGetValue("face", out var face))
                        name = face;
                    break;

                case "common":
                    lineHeight = ReadFloat(fields, "lineHeight", lineHeight);
                    baseLine = ReadFloat(fields, "base", baseLine);
                    break;

                case "page":
                    if (fields.TryGetValue("file", out var pageFile))
                        atlasFile = pageFile;
                    break;

                case "char":
                    var character = (char)ReadInt(fields, "id", 0);
                    var source = new Rectangle(
                        ReadInt(fields, "x", 0),
                        ReadInt(fields, "y", 0),
                        ReadInt(fields, "width", 0),
                        ReadInt(fields, "height", 0));

                    if (source.Width <= 0 || source.Height <= 0)
                        continue;

                    var glyph = new FontGlyph(
                        source,
                        ReadFloat(fields, "xadvance", source.Width),
                        new Vector2(
                            ReadFloat(fields, "xoffset", 0),
                            ReadFloat(fields, "yoffset", 0)));

                    data.Glyphs[character] = glyph;
                    break;
            }
        }

        if (data.Glyphs.Count == 0)
            throw new InvalidDataException("The font map has no characters.");

        if (lineHeight <= 0)
            lineHeight = data.Glyphs.Values.Max(glyph => glyph.Source.Height);

        var result = new BmFontData
        {
            Name = string.IsNullOrWhiteSpace(name) ? "font" : name,
            AtlasFile = atlasFile,
            LineHeight = lineHeight,
            BaseLine = baseLine <= 0 ? lineHeight : baseLine,
        };

        foreach (var pair in data.Glyphs)
            result.Glyphs[pair.Key] = pair.Value;

        return result;
    }

    public static BitmapFont Load(Renderer renderer, string mapPath)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapPath);

        var fullPath = Path.GetFullPath(mapPath);
        var data = Parse(File.ReadAllText(fullPath));

        if (string.IsNullOrWhiteSpace(data.AtlasFile))
            throw new InvalidDataException($"'{mapPath}' does not name an atlas page.");

        var atlasPath = Path.Combine(
            Path.GetDirectoryName(fullPath) ?? string.Empty,
            data.AtlasFile);

        return new BitmapFont(
            data.Name,
            renderer.LoadTexture(atlasPath),
            data.LineHeight,
            data.BaseLine,
            data.Glyphs);
    }

    private static Dictionary<string, string> ParseFields(string line)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var index = line.IndexOf(' ');
        if (index < 0)
            return fields;

        while (index < line.Length)
        {
            while (index < line.Length && line[index] == ' ')
                index++;

            if (index >= line.Length)
                break;

            var keyStart = index;
            while (index < line.Length && line[index] != '=')
                index++;

            if (index >= line.Length)
                break;

            var key = line[keyStart..index].Trim();
            index++; // '='

            string value;
            if (index < line.Length && line[index] == '"')
            {
                index++;
                var valueStart = index;
                while (index < line.Length && line[index] != '"')
                    index++;
                value = line[valueStart..index];
                if (index < line.Length)
                    index++;
            }
            else
            {
                var valueStart = index;
                while (index < line.Length && line[index] != ' ')
                    index++;
                value = line[valueStart..index];
            }

            if (key.Length > 0)
                fields[key] = value;
        }

        return fields;
    }

    private static int ReadInt(Dictionary<string, string> fields, string key, int fallback)
        => fields.TryGetValue(key, out var value) &&
           int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;

    private static float ReadFloat(Dictionary<string, string> fields, string key, float fallback)
        => fields.TryGetValue(key, out var value) &&
           float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
}
