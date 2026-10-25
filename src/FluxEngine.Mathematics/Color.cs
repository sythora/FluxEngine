namespace FluxEngine.Mathematics;

// plain byte color, the same values SDL takes so no conversions needed
public readonly struct Color(byte r, byte g, byte b, byte a = 255)
{
    public byte R { get; } = r;
    public byte G { get; } = g;
    public byte B { get; } = b;
    public byte A { get; } = a;

    public static Color White => new(255, 255, 255);
    public static Color Black => new(0, 0, 0);
    public static Color Red => new(220, 60, 55);
    public static Color Green => new(70, 190, 90);
    public static Color Blue => new(60, 120, 220);
    public static Color Yellow => new(250, 210, 60);
    public static Color Orange => new(250, 140, 50);
    public static Color Purple => new(150, 80, 200);
    public static Color Pink => new(250, 120, 180);
    public static Color Cyan => new(70, 200, 220);
    public static Color Magenta => new(220, 70, 200);
    public static Color Brown => new(140, 90, 50);
    public static Color Gray => new(140, 140, 140);
    public static Color LightGray => new(200, 200, 200);
    public static Color DarkGray => new(80, 80, 80);
    public static Color CornflowerBlue => new(100, 150, 255);
    public static Color Gold => new(255, 215, 0);
    public static Color Transparent => new(0, 0, 0, 0);

    // accepts "ff8800", "#ff8800" or the short form "f80"
    public static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 3)
        {
            return new Color(
                ParseHex(hex[0..1] + hex[0..1]),
                ParseHex(hex[1..2] + hex[1..2]),
                ParseHex(hex[2..3] + hex[2..3]));
        }
        if (hex.Length >= 6)
        {
            var color = new Color(
                ParseHex(hex[0..2]),
                ParseHex(hex[2..4]),
                ParseHex(hex[4..6]));
            return hex.Length >= 8 ? color.WithAlpha(ParseHex(hex[6..8])) : color;
        }
        throw new ArgumentException($"'{hex}' is not a valid hex color.", nameof(hex));
    }

    private static byte ParseHex(string digits)
    {
        return Convert.ToByte(digits, 16);
    }

    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";

    public Color WithAlpha(byte alpha) => new(R, G, B, alpha);

    public static Color Lerp(Color from, Color to, float t)
    {
        t = MathUtil.Clamp01(t);
        return new Color(
            (byte)MathF.Round(MathUtil.Lerp(from.R, to.R, t)),
            (byte)MathF.Round(MathUtil.Lerp(from.G, to.G, t)),
            (byte)MathF.Round(MathUtil.Lerp(from.B, to.B, t)),
            (byte)MathF.Round(MathUtil.Lerp(from.A, to.A, t)));
    }

    public static bool operator ==(Color a, Color b) => a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
    public static bool operator !=(Color a, Color b) => !(a == b);

    public bool Equals(Color other) => this == other;
    public override bool Equals(object? obj) => obj is Color other && this == other;
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);
    public override string ToString() => ToHex();
}
