using System.IO;
using FluxEngine.Mathematics;

// Generates BMP texture files at runtime. SDL core only loads BMP, so we
// create pixel-art textures procedurally instead of relying on external art
// files that may not exist on the user's machine.
public static class BmpGenerator
{
    private static readonly string AssetsDir = Path.Combine(
        Directory.GetCurrentDirectory(), "assets");

    static BmpGenerator()
    {
        Directory.CreateDirectory(AssetsDir);
    }

    public static string PlayerIdle()
        => Write("player_idle.bmp", 8, 16, PlayerIdlePixels);

    public static string PlayerRun0()
        => Write("player_run_0.bmp", 8, 16, PlayerRun0Pixels);

    public static string PlayerRun1()
        => Write("player_run_1.bmp", 8, 16, PlayerRun1Pixels);

    public static string PlayerJump()
        => Write("player_jump.bmp", 8, 16, PlayerJumpPixels);

    public static string Platform(int width, int height)
        => Write($"platform_{width}x{height}.bmp", width, height,
            (x, y) => PlatformPixels(x, y, width, height));

    public static string Coin(int frame)
        => Write($"coin_{frame}.bmp", 8, 8, (x, y) => CoinPixels(x, y, frame));

    private static string Write(string name, int width, int height,
        Func<int, int, Color> pixel)
    {
        var path = Path.Combine(AssetsDir, name);

        if (File.Exists(path))
            return path;

        var rowSize = (width * 3 + 3) & ~3;
        var imageSize = rowSize * height;
        var fileSize = 54 + imageSize;

        using var fs = File.Create(path);
        using var writer = new BinaryWriter(fs);

        // BMP file header
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write((short)0);
        writer.Write((short)0);
        writer.Write(54);

        // DIB header
        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1);
        writer.Write((short)24);
        writer.Write(0);
        writer.Write(imageSize);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        // Pixel data, bottom-up
        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var c = pixel(x, y);
                writer.Write(c.B);
                writer.Write(c.G);
                writer.Write(c.R);
            }

            var pad = rowSize - width * 3;
            for (var p = 0; p < pad; p++)
                writer.Write((byte)0);
        }

        return path;
    }

    // Player character: head, body, legs, arm. Pixel art.
    private static Color PlayerIdlePixels(int x, int y)
    {
        var c = Color.Transparent;

        // Head (skin)
        if (y >= 1 && y <= 4 && x >= 2 && x <= 5)
            c = new Color(255, 220, 175);
        // Hair
        if (y == 1 && x >= 2 && x <= 5)
            c = new Color(60, 40, 30);
        // Eyes
        if (y == 3 && (x == 3 || x == 4))
            c = new Color(20, 20, 20);
        // Body (blue shirt)
        if (y >= 5 && y <= 10 && x >= 2 && x <= 5)
            c = new Color(70, 130, 220);
        // Arms
        if (y >= 5 && y <= 7 && x == 1)
            c = new Color(70, 130, 220);
        if (y >= 5 && y <= 7 && x == 6)
            c = new Color(70, 130, 220);
        // Legs (brown pants)
        if (y >= 11 && y <= 14)
        {
            if (x == 2 || x == 3)
                c = new Color(130, 90, 50);
            if (x == 4 || x == 5)
                c = new Color(130, 90, 50);
        }
        // Feet (darker brown)
        if (y == 15 && (x == 2 || x == 3 || x == 4 || x == 5))
            c = new Color(90, 60, 30);

        return c;
    }

    private static Color PlayerRun0Pixels(int x, int y)
    {
        var c = PlayerIdlePixels(x, y);

        // Left leg forward
        if (y >= 11 && y <= 14 && x == 1)
            c = new Color(130, 90, 50);
        if (y == 15 && x == 1)
            c = new Color(90, 60, 30);
        // Right leg back
        if (y >= 11 && y <= 12 && x == 5)
            c = Color.Transparent;
        if (y == 13 && x == 5)
            c = new Color(130, 90, 50);
        if (y == 14 && x == 5)
            c = new Color(130, 90, 50);
        if (y == 15 && x == 5)
            c = new Color(90, 60, 30);

        return c;
    }

    private static Color PlayerRun1Pixels(int x, int y)
    {
        var c = PlayerIdlePixels(x, y);

        // Right leg forward
        if (y >= 11 && y <= 14 && x == 5)
            c = new Color(130, 90, 50);
        if (y == 15 && x == 5)
            c = new Color(90, 60, 30);
        // Left leg back
        if (y >= 11 && y <= 12 && x == 2)
            c = Color.Transparent;
        if (y == 13 && x == 2)
            c = new Color(130, 90, 50);
        if (y == 14 && x == 2)
            c = new Color(130, 90, 50);
        if (y == 15 && x == 2)
            c = new Color(90, 60, 30);

        return c;
    }

    private static Color PlayerJumpPixels(int x, int y)
    {
        var c = Color.Transparent;

        // Head
        if (y >= 1 && y <= 4 && x >= 2 && x <= 5)
            c = new Color(255, 220, 175);
        if (y == 1 && x >= 2 && x <= 5)
            c = new Color(60, 40, 30);
        if (y == 3 && (x == 3 || x == 4))
            c = new Color(20, 20, 20);
        // Body
        if (y >= 5 && y <= 10 && x >= 2 && x <= 5)
            c = new Color(70, 130, 220);
        // Arms up
        if (y == 5 && x == 1)
            c = new Color(70, 130, 220);
        if (y == 5 && x == 6)
            c = new Color(70, 130, 220);
        // Legs together
        if (y >= 11 && y <= 14 && x >= 3 && x <= 4)
            c = new Color(130, 90, 50);
        if (y == 15 && x >= 3 && x <= 4)
            c = new Color(90, 60, 30);

        return c;
    }

    // Platform: grass top, dirt body, dark edges
    private static Color PlatformPixels(int x, int y, int width, int height)
    {
        var grassBottom = 4;
        var dirtTop = grassBottom;
        var edge = 1;

        // Top edge
        if (y == 0)
            return new Color(60, 100, 50);

        // Grass
        if (y >= 1 && y <= grassBottom)
        {
            if (x == 0 || x == width - 1)
                return new Color(40, 70, 30);
            return new Color(80, 160, 60);
        }

        // Dirt
        if (y > grassBottom && y < height - 1)
        {
            if (x == 0 || x == width - 1)
                return new Color(90, 50, 20);
            return new Color(140, 90, 50);
        }

        // Bottom edge
        if (y == height - 1)
            return new Color(60, 40, 15);

        return Color.Transparent;
    }

    // Coin: yellow circle with highlight, animated spin
    private static Color CoinPixels(int x, int y, int frame)
    {
        var cx = 3.5f;
        var cy = 3.5f;
        var dx = x - cx;
        var dy = y - cy;
        var dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist > 3.5f)
            return Color.Transparent;

        // Shading based on frame (spin effect)
        var shade = frame switch
        {
            0 => new Color(255, 220, 0),
            1 => new Color(255, 180, 0),
            2 => new Color(200, 150, 0),
            _ => new Color(150, 100, 0),
        };

        // Highlight
        if (dx < -0.5f && dy < -0.5f)
            return new Color(255, 255, 150);

        return shade;
    }
}
