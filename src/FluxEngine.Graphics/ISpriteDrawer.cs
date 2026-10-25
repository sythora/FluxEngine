using FluxEngine.Graphics;

// anything that can be drawn as a sprite and sorted by layer
internal interface ISpriteDrawer
{
    int Layer { get; }
    void OnDraw(Renderer renderer);
}
