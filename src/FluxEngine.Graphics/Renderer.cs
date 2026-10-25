using SDL2;
using FluxEngine.Mathematics;

namespace FluxEngine.Graphics;

// wrappers around SDL draw calls. sizes are in world units and the camera
// handles turning those into screen pixels
public sealed class Renderer
{
    private readonly IntPtr _windowHandle;
    private readonly IntPtr _rendererHandle;

    internal Renderer(IntPtr windowHandle, IntPtr rendererHandle)
    {
        _windowHandle = windowHandle;
        _rendererHandle = rendererHandle;
    }

    // scene sets this every frame
    public Camera? Camera { get; set; }

    private float Zoom => Camera is { Zoom: > 0 } camera ? camera.Zoom : 1f;

    public void Clear(Color color)
    {
        SDL.SDL_SetRenderDrawColor(_rendererHandle, color.R, color.G, color.B, color.A);
        SDL.SDL_RenderClear(_rendererHandle);
    }

    public void DrawRectangle(Rectangle rectangle, Color color)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
            return;

        var screen = ToScreen(rectangle.Position);
        var sdlRect = new SDL.SDL_Rect
        {
            x = (int)screen.X,
            y = (int)screen.Y,
            w = (int)(rectangle.Width * Zoom),
            h = (int)(rectangle.Height * Zoom),
        };

        SetColor(color);
        SDL.SDL_RenderFillRect(_rendererHandle, ref sdlRect);
    }

    public void DrawRectangleOutline(Rectangle rectangle, Color color)
    {
        var screen = ToScreen(rectangle.Position);
        var sdlRect = new SDL.SDL_Rect
        {
            x = (int)screen.X,
            y = (int)screen.Y,
            w = (int)(rectangle.Width * Zoom),
            h = (int)(rectangle.Height * Zoom),
        };

        SetColor(color);
        SDL.SDL_RenderDrawRect(_rendererHandle, ref sdlRect);
    }

    public void DrawLine(Vector2 from, Vector2 to, Color color)
    {
        var start = ToScreen(from);
        var end = ToScreen(to);

        SetColor(color);
        SDL.SDL_RenderDrawLine(
            _rendererHandle,
            (int)start.X,
            (int)start.Y,
            (int)end.X,
            (int)end.Y);
    }

    // no SDL shape calls for circles, so this fills one row at a time
    public void DrawCircle(Vector2 center, float radius, Color color)
    {
        var screen = ToScreen(center);
        var pixelRadius = (int)(radius * Zoom);

        if (pixelRadius <= 0)
            return;

        var centerX = (int)screen.X;
        var centerY = (int)screen.Y;

        SetColor(color);

        for (var dy = -pixelRadius; dy <= pixelRadius; dy++)
        {
            var halfWidth = MathF.Sqrt(pixelRadius * pixelRadius - dy * dy);
            SDL.SDL_RenderDrawLine(
                _rendererHandle,
                centerX - (int)halfWidth,
                centerY + dy,
                centerX + (int)halfWidth,
                centerY + dy);
        }
    }

    public void DrawCircleOutline(Vector2 center, float radius, Color color, int segments = 32)
    {
        var screen = ToScreen(center);
        var pixelRadius = (int)(radius * Zoom);

        if (pixelRadius <= 0)
            return;

        segments = Math.Max(8, segments);
        SetColor(color);

        var previous = new Vector2(screen.X + pixelRadius, screen.Y);
        var step = MathF.Tau / segments;

        for (var i = 1; i <= segments; i++)
        {
            var angle = step * i;
            var current = new Vector2(
                screen.X + MathF.Cos(angle) * pixelRadius,
                screen.Y + MathF.Sin(angle) * pixelRadius);

            SDL.SDL_RenderDrawLine(
                _rendererHandle,
                (int)previous.X,
                (int)previous.Y,
                (int)current.X,
                (int)current.Y);

            previous = current;
        }
    }

    // closed shape from a list of points, lines only, no fill
    public void DrawPolygon(IReadOnlyList<Vector2> points, Color color)
    {
        if (points.Count < 2)
            return;

        SetColor(color);

        for (var i = 0; i < points.Count; i++)
        {
            var start = ToScreen(points[i]);
            var end = ToScreen(points[(i + 1) % points.Count]);

            SDL.SDL_RenderDrawLine(
                _rendererHandle,
                (int)start.X,
                (int)start.Y,
                (int)end.X,
                (int)end.Y);
        }
    }

    // position is the middle of the sprite, rotation is degrees
    public void DrawSprite(
        Sprite sprite,
        Vector2 position,
        float rotation = 0,
        Vector2? scale = null,
        Color? tint = null)
    {
        ArgumentNullException.ThrowIfNull(sprite);

        var texture = sprite.Texture;

        if (texture is null)
            return;

        var drawScale = (scale ?? Vector2.One) * Zoom;
        var source = sprite.Source ?? texture.Bounds;
        var width = (int)(source.Width * drawScale.X);
        var height = (int)(source.Height * drawScale.Y);

        if (width <= 0 || height <= 0)
            return;

        var screen = ToScreen(position);

        var sdlSource = new SDL.SDL_Rect
        {
            x = (int)source.X,
            y = (int)source.Y,
            w = (int)source.Width,
            h = (int)source.Height,
        };

        var destination = new SDL.SDL_Rect
        {
            x = (int)(screen.X - width * 0.5f),
            y = (int)(screen.Y - height * 0.5f),
            w = width,
            h = height,
        };

        var center = new SDL.SDL_Point { x = width / 2, y = height / 2 };
        var flip = sprite.FlipX
            ? sprite.FlipY
                ? SDL.SDL_RendererFlip.SDL_FLIP_HORIZONTAL | SDL.SDL_RendererFlip.SDL_FLIP_VERTICAL
                : SDL.SDL_RendererFlip.SDL_FLIP_HORIZONTAL
            : sprite.FlipY
                ? SDL.SDL_RendererFlip.SDL_FLIP_VERTICAL
                : SDL.SDL_RendererFlip.SDL_FLIP_NONE;

        var color = tint ?? Color.White;
        SDL.SDL_SetTextureColorMod(texture.Handle, color.R, color.G, color.B);
        SDL.SDL_SetTextureAlphaMod(texture.Handle, color.A);

        SDL.SDL_RenderCopyEx(
            _rendererHandle,
            texture.Handle,
            ref sdlSource,
            ref destination,
            rotation,
            ref center,
            flip);

        // snap the tint back or the next thing using this texture comes out wrong
        SDL.SDL_SetTextureColorMod(texture.Handle, 255, 255, 255);
        SDL.SDL_SetTextureAlphaMod(texture.Handle, 255);
    }

    // bmp only for now, SDL core has no png decoder. that is what SDL_image is for
    public Texture LoadTexture(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"No texture at '{path}'.", path);

        var surface = SDL.SDL_LoadBMP(path);

        if (surface == IntPtr.Zero)
            throw new InvalidOperationException($"Couldn't load '{path}': {SDL.SDL_GetError()}");

        try
        {
            return CreateTextureFromSurface(surface);
        }
        finally
        {
            SDL.SDL_FreeSurface(surface);
        }
    }

    internal Texture CreateTextureFromSurface(IntPtr surface)
    {
        var handle = SDL.SDL_CreateTextureFromSurface(_rendererHandle, surface);

        if (handle == IntPtr.Zero)
            throw new InvalidOperationException($"Couldn't create a texture: {SDL.SDL_GetError()}");

        SDL.SDL_QueryTexture(handle, out _, out _, out var width, out var height);
        return new Texture(handle, width, height);
    }

    private Vector2 ToScreen(Vector2 world)
    {
        if (Camera is null)
            return world;

        SDL.SDL_GetWindowSize(_windowHandle, out var width, out var height);
        return Camera.WorldToScreen(world, width, height);
    }

    private void SetColor(Color color)
        => SDL.SDL_SetRenderDrawColor(_rendererHandle, color.R, color.G, color.B, color.A);
}
