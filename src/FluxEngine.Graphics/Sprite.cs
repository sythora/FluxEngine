using FluxEngine.Mathematics;

namespace FluxEngine.Graphics;

// a texture, optionally a chunk of one. flip flags are for spritesheets
public sealed class Sprite
{
    public Texture? Texture { get; set; }

    // null means the whole texture
    public Rectangle? Source { get; set; }

    public bool FlipX { get; set; }
    public bool FlipY { get; set; }

    public int Width => (int)(Source?.Width ?? Texture?.Width ?? 0);
    public int Height => (int)(Source?.Height ?? Texture?.Height ?? 0);

    public Sprite() { }
    public Sprite(Texture texture) => Texture = texture;

    // easy way to scrape frames off a sheet with equal size cells
    public Sprite WithFrame(int column, int row, int frameWidth, int frameHeight) => new()
    {
        Texture = Texture,
        Source = new Rectangle(column * frameWidth, row * frameHeight, frameWidth, frameHeight),
        FlipX = FlipX,
        FlipY = FlipY,
    };
}
