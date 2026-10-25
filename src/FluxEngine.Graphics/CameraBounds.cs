using FluxEngine.Graphics;
using FluxEngine.Mathematics;

namespace FluxEngine.Graphics;

// keeps the camera from wandering outside the level. set Bounds on the camera
public static class CameraBounds
{
    public static void Clamp(Camera camera, Rectangle levelBounds, int viewWidth, int viewHeight)
    {
        if (camera is null)
            return;

        var halfW = viewWidth * 0.5f / camera.Zoom;
        var halfH = viewHeight * 0.5f / camera.Zoom;

        var minX = levelBounds.X + halfW;
        var maxX = levelBounds.Right - halfW;
        var minY = levelBounds.Y + halfH;
        var maxY = levelBounds.Bottom - halfH;

        camera.Position = new Vector2(
            MathUtil.Clamp(camera.Position.X, minX, maxX),
            MathUtil.Clamp(camera.Position.Y, minY, maxY));
    }
}
