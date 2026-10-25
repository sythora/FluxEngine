using FluxEngine.Core;
using FluxEngine.Mathematics;

namespace FluxEngine.Graphics;

// draws a sprite or a plain box. set Sprite and it's a sprite, leave it null
// and you get a colored rectangle, handy while there's no art yet
public sealed class SpriteRenderer : Component, ISpriteDrawer
{
    public Sprite? Sprite { get; set; }

    // box size and tint, sprite size comes from the texture
    public Vector2 Size { get; set; } = new(32, 32);
    public Color Color { get; set; } = Color.White;

    // bigger layer draws later, on top
    public int Layer { get; set; }

    public override void OnDraw(Renderer renderer)
    {
        var scale = new Vector2(MathF.Abs(Entity.Scale.X), MathF.Abs(Entity.Scale.Y));

        if (Sprite?.Texture is not null)
        {
            renderer.DrawSprite(Sprite, Entity.Position, Entity.Rotation, scale, Color);
            return;
        }

        var rectangle = Rectangle.FromCenter(Entity.Position, Size * scale);
        renderer.DrawRectangle(rectangle, Color);
    }
}
