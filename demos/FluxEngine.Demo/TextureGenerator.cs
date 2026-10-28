using System.IO;
using FluxEngine.Graphics;
using FluxEngine.Mathematics;

// Generates PNG texture files at runtime. Keeping the demo self contained
// means there are no external art files to hunt down before pressing run.
public static class TextureGenerator
{
    private static readonly string AssetsDir = Path.Combine(
        Directory.GetCurrentDirectory(), "assets");

    static TextureGenerator()
    {
        Directory.CreateDirectory(AssetsDir);
    }

    public static string PlayerIdle()
        => Write("player_idle.png", 16, 28, PlayerIdlePixels);

    public static string PlayerRun0()
        => Write("player_run_0.png", 16, 28, PlayerRun0Pixels);

    public static string PlayerRun1()
        => Write("player_run_1.png", 16, 28, PlayerRun1Pixels);

    public static string PlayerJump()
        => Write("player_jump.png", 16, 28, PlayerJumpPixels);

    public static string Platform(int width, int height)
        => Write($"platform_{width}x{height}.png", width, height,
            (x, y) => PlatformPixels(x, y, width, height));

    public static string Coin(int frame)
        => Write($"coin_{frame}.png", 8, 8, (x, y) => CoinPixels(x, y, frame));

    private static string Write(string name, int width, int height,
        Func<int, int, Color> pixel)
    {
        var path = Path.Combine(AssetsDir, name);
        var rgba = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var color = pixel(x, y);
                var index = (y * width + x) * 4;
                rgba[index] = color.R;
                rgba[index + 1] = color.G;
                rgba[index + 2] = color.B;
                rgba[index + 3] = color.A;
            }
        }

        PngWriter.Write(path, width, height, rgba);
        return path;
    }

    // Base character structure: 16x28 pixel-art hero runner.
    // X: 0 to 15, Y: 0 to 27
    private static Color PlayerIdlePixels(int x, int y)
    {
        // Hair (rich chestnut with highlight strands)
        if (y == 2 && x >= 5 && x <= 10) return new Color(90, 50, 30);
        if (y == 3 && x >= 4 && x <= 12) return (x == 6 || x == 7) ? new Color(130, 80, 50) : new Color(70, 40, 25);
        if (y == 4 && x >= 4 && x <= 12) return (x == 5 || x == 11) ? new Color(60, 35, 20) : (x == 8 ? new Color(130, 80, 50) : new Color(80, 45, 30));
        if (y == 5 && ((x >= 3 && x <= 5) || (x >= 11 && x <= 12))) return new Color(60, 35, 20);

        // Face & Head (warm peach tone)
        if (y >= 5 && y <= 9 && x >= 6 && x <= 11)
        {
            // Eyes with highlights
            if (y == 7)
            {
                if (x == 8 || x == 10) return new Color(25, 30, 45); // pupils
                if (x == 9 || x == 11) return new Color(255, 255, 255); // eye white
            }
            // Cheek blush
            if (y == 8 && (x == 6 || x == 11)) return new Color(245, 170, 160);
            // Mouth
            if (y == 9 && x == 8) return new Color(200, 130, 120);

            return new Color(255, 218, 185);
        }

        // Red hero scarf / bandana around neck
        if (y == 10 && x >= 4 && x <= 12) return new Color(225, 35, 45);
        if (y == 11 && x >= 5 && x <= 11) return new Color(195, 25, 35);
        // Scarf tail trailing behind to the left
        if (y == 11 && x >= 1 && x <= 4) return new Color(225, 35, 45);
        if (y == 12 && x >= 0 && x <= 3) return new Color(180, 20, 30);

        // Runner Jacket (vibrant blue with crisp white trim)
        if (y >= 12 && y <= 17 && x >= 5 && x <= 11)
        {
            if (x == 8) return new Color(245, 245, 255); // zipper stripe
            if (x == 5 || x == 11) return new Color(30, 75, 170); // outer shading
            return new Color(50, 120, 230); // jacket blue
        }

        // Arms & Hands (Idle)
        if (y >= 12 && y <= 15 && (x == 4 || x == 12)) return new Color(40, 95, 195);
        if (y >= 16 && y <= 17 && (x == 4 || x == 12)) return new Color(255, 218, 185); // hands

        // Belt
        if (y == 18 && x >= 5 && x <= 11)
        {
            if (x == 8) return new Color(245, 200, 45); // gold buckle
            return new Color(35, 35, 42); // dark leather
        }

        // Pants / Joggers (charcoal runner pants)
        if (y >= 19 && y <= 24)
        {
            // Left leg
            if (x >= 5 && x <= 7) return y == 21 ? new Color(75, 80, 95) : new Color(50, 55, 65);
            // Right leg
            if (x >= 9 && x <= 11) return y == 21 ? new Color(75, 80, 95) : new Color(50, 55, 65);
        }

        // Sneakers (red & white high-tops)
        if (y >= 25 && y <= 27)
        {
            // Left shoe
            if (x >= 4 && x <= 7)
            {
                if (y == 25) return new Color(220, 35, 45); // red upper
                if (y == 26) return new Color(245, 245, 250); // white rubber sole
                if (y == 27) return new Color(35, 35, 40); // dark bottom tread
            }
            // Right shoe
            if (x >= 9 && x <= 12)
            {
                if (y == 25) return new Color(220, 35, 45);
                if (y == 26) return new Color(245, 245, 250);
                if (y == 27) return new Color(35, 35, 40);
            }
        }

        return Color.Transparent;
    }

    private static Color PlayerRun0Pixels(int x, int y)
    {
        if (y <= 18)
        {
            // Wind-blown scarf tail
            if (y == 10 && x >= 0 && x <= 4) return new Color(225, 35, 45);
            if (y == 11 && x >= 0 && x <= 3) return new Color(195, 25, 35);

            // Right arm swinging forward
            if (y >= 13 && y <= 15 && x == 13) return new Color(40, 95, 195);
            if (y >= 16 && y <= 17 && x == 13) return new Color(255, 218, 185);

            // Left arm swinging back
            if (y >= 12 && y <= 14 && x == 3) return new Color(40, 95, 195);
            if (y >= 15 && y <= 16 && x == 3) return new Color(255, 218, 185);

            if (x == 4 && y >= 12 && y <= 17) return Color.Transparent;
            if (x == 12 && y >= 12 && y <= 17) return Color.Transparent;

            return PlayerIdlePixels(x, y);
        }

        // Pants / Legs in dynamic running stride:
        // Left leg kicking forward (X: 8 to 12)
        if (y >= 19 && y <= 24 && x >= 8 && x <= 12) return new Color(55, 60, 72);
        // Right leg pushing backward (X: 3 to 6)
        if (y >= 19 && y <= 23 && x >= 3 && x <= 6) return new Color(40, 45, 55);

        // Shoes in running pose:
        // Front foot forward planted
        if (y >= 25 && y <= 27 && x >= 9 && x <= 14)
        {
            if (y == 25) return new Color(220, 35, 45);
            if (y == 26) return new Color(245, 245, 250);
            return new Color(35, 35, 40);
        }
        // Back foot angled up/back
        if (y >= 22 && y <= 25 && x >= 1 && x <= 4)
        {
            if (y == 22 || y == 23) return new Color(220, 35, 45);
            if (y == 24) return new Color(245, 245, 250);
            return new Color(35, 35, 40);
        }

        return Color.Transparent;
    }

    private static Color PlayerRun1Pixels(int x, int y)
    {
        // 1px vertical bob during run step
        if (y <= 17)
        {
            var baseColor = PlayerIdlePixels(x, y + 1);
            if (y == 11 && x >= 0 && x <= 3) return new Color(225, 35, 45);
            return baseColor;
        }

        if (y == 18 && x >= 5 && x <= 11)
        {
            if (x == 8) return new Color(245, 200, 45);
            return new Color(35, 35, 42);
        }

        // Crossing legs
        if (y >= 19 && y <= 24 && x >= 6 && x <= 10)
        {
            return (x <= 7) ? new Color(55, 60, 72) : new Color(40, 45, 55);
        }

        // Crossing feet
        if (y >= 25 && y <= 27 && x >= 6 && x <= 11)
        {
            if (y == 25) return new Color(220, 35, 45);
            if (y == 26) return new Color(245, 245, 250);
            return new Color(35, 35, 40);
        }

        return Color.Transparent;
    }

    private static Color PlayerJumpPixels(int x, int y)
    {
        // Airborne hero leap
        if (y <= 10)
        {
            var c = PlayerIdlePixels(x, y);
            // Scarf flying upward-back in wind
            if (y == 9 && x >= 1 && x <= 4) return new Color(225, 35, 45);
            if (y == 8 && x >= 0 && x <= 2) return new Color(195, 25, 35);
            return c;
        }

        // Torso
        if (y >= 11 && y <= 16 && x >= 5 && x <= 11)
        {
            if (x == 8) return new Color(245, 245, 255);
            return new Color(50, 120, 230);
        }

        // Arms reaching up/forward for momentum
        if (y >= 9 && y <= 12 && (x == 3 || x == 13)) return new Color(40, 95, 195);
        if (y >= 7 && y <= 8 && (x == 3 || x == 13)) return new Color(255, 218, 185);

        // Belt
        if (y == 17 && x >= 5 && x <= 11)
        {
            if (x == 8) return new Color(245, 200, 45);
            return new Color(35, 35, 42);
        }

        // Legs tucked up in jump
        if (y >= 18 && y <= 22 && x >= 4 && x <= 12)
        {
            if (x >= 5 && x <= 7) return new Color(60, 65, 80);
            if (x >= 9 && x <= 11) return new Color(45, 50, 62);
        }

        // Shoes tucked up
        if (y >= 23 && y <= 25 && x >= 4 && x <= 12)
        {
            if (y == 23) return new Color(220, 35, 45);
            if (y == 24) return new Color(245, 245, 250);
            return new Color(35, 35, 40);
        }

        return Color.Transparent;
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
