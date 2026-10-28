using FluxEngine.Mathematics;

namespace FluxEngine.Graphics;

// One character in a bitmap font atlas. Source is the cell in the texture and
// Offset moves the cell relative to the text cursor before it is drawn.
public readonly record struct FontGlyph(Rectangle Source, float Advance, Vector2 Offset);

// Bitmap font backed by a texture atlas. Load one from a BMFont .fnt map with
// BmFont.Load, or use one of the built-in fonts in FluxEngine.Content.
public sealed class BitmapFont : IDisposable
{
    private readonly Dictionary<char, FontGlyph> _glyphs;
    private readonly char? _fallback;

    public BitmapFont(
        string name,
        Texture atlas,
        float lineHeight,
        float baseLine,
        IDictionary<char, FontGlyph> glyphs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(atlas);
        ArgumentNullException.ThrowIfNull(glyphs);
        if (lineHeight <= 0 || !float.IsFinite(lineHeight))
            throw new ArgumentOutOfRangeException(nameof(lineHeight), "Line height must be positive.");
        if (glyphs.Count == 0)
            throw new ArgumentException("A font needs at least one glyph.", nameof(glyphs));

        Name = name;
        Atlas = atlas;
        LineHeight = lineHeight;
        BaseLine = baseLine;
        _glyphs = new Dictionary<char, FontGlyph>(glyphs);
        _fallback = _glyphs.ContainsKey('?') ? '?' : null;
    }

    public string Name { get; }
    public Texture Atlas { get; }
    public float LineHeight { get; }
    public float BaseLine { get; }
    public int GlyphCount => _glyphs.Count;
    public IReadOnlyDictionary<char, FontGlyph> Glyphs => _glyphs;

    public bool HasGlyph(char character) => _glyphs.ContainsKey(character);

    // position is the top left of the first line in world/screen units. draw
    // after clearing renderer.Camera for HUD text so it does not scroll.
    public void Draw(
        Renderer renderer,
        string text,
        Vector2 position,
        Color? color = null,
        float scale = 1f)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(text);
        if (!float.IsFinite(scale) || scale <= 0)
            throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be finite and greater than zero.");

        var x = position.X;
        var y = position.Y;

        foreach (var character in text)
        {
            if (character == '\n')
            {
                x = position.X;
                y += LineHeight * scale;
                continue;
            }

            if (character == '\r')
                continue;

            if (!_glyphs.TryGetValue(character, out var glyph))
            {
                if (_fallback is null)
                    continue;

                glyph = _glyphs[_fallback.Value];
            }

            var source = glyph.Source;
            var destination = new Rectangle(
                x + glyph.Offset.X * scale,
                y + glyph.Offset.Y * scale,
                source.Width * scale,
                source.Height * scale);

            renderer.DrawTexture(Atlas, source, destination, color);
            x += glyph.Advance * scale;
        }
    }

    public Vector2 Measure(string text, float scale = 1f)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!float.IsFinite(scale) || scale <= 0)
            throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be finite and greater than zero.");

        var widest = 0f;
        var x = 0f;
        var lines = 1;

        foreach (var character in text)
        {
            if (character == '\n')
            {
                widest = MathF.Max(widest, x);
                x = 0f;
                lines++;
                continue;
            }

            if (character == '\r')
                continue;

            if (!_glyphs.TryGetValue(character, out var glyph))
            {
                if (_fallback is null)
                    continue;

                glyph = _glyphs[_fallback.Value];
            }

            x += glyph.Advance;
        }

        widest = MathF.Max(widest, x);
        return new Vector2(widest * scale, lines * LineHeight * scale);
    }

    public float MeasureWidth(string text, float scale = 1f) => Measure(text, scale).X;

    // the atlas belongs to this font, so disposing the font kills it
    public void Dispose() => Atlas.Dispose();
}
