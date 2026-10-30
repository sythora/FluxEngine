using FluxEngine.Mathematics;

namespace FluxEngine.Graphics;

public sealed record BrowserDrawCommand(
    string Type,
    float[] Values,
    Color Color,
    Color SecondaryColor,
    string? Text = null,
    bool ScreenSpace = false);
