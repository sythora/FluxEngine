using SDL2;
using System.Runtime.InteropServices;
using FluxEngine.Mathematics;
using StbImageSharp;

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

        // alpha blending for all draw primitives (rects, lines, etc.)
        SDL.SDL_SetRenderDrawBlendMode(_rendererHandle, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
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

    // draws a sub-rectangle of a texture with a top-left anchor. used by fonts
    // and tilemaps, which need exact pixel placement instead of sprite centers
    public void DrawTexture(
        Texture texture,
        Rectangle source,
        Rectangle destination,
        Color? tint = null)
    {
        ArgumentNullException.ThrowIfNull(texture);

        if (source.Width <= 0 || source.Height <= 0 ||
            destination.Width <= 0 || destination.Height <= 0)
            return;

        var screen = ToScreen(destination.Position);

        var sdlSource = new SDL.SDL_Rect
        {
            x = (int)source.X,
            y = (int)source.Y,
            w = (int)source.Width,
            h = (int)source.Height,
        };

        var sdlDestination = new SDL.SDL_Rect
        {
            x = (int)screen.X,
            y = (int)screen.Y,
            w = (int)(destination.Width * Zoom),
            h = (int)(destination.Height * Zoom),
        };

        var color = tint ?? Color.White;
        SDL.SDL_SetTextureColorMod(texture.Handle, color.R, color.G, color.B);
        SDL.SDL_SetTextureAlphaMod(texture.Handle, color.A);

        SDL.SDL_RenderCopy(_rendererHandle, texture.Handle, ref sdlSource, ref sdlDestination);

        SDL.SDL_SetTextureColorMod(texture.Handle, 255, 255, 255);
        SDL.SDL_SetTextureAlphaMod(texture.Handle, 255);
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

    // PNG and JPEG go through stb_image, BMP uses SDL's native loader. both
    // paths end up as one SDL texture with alpha blending enabled.
    public Texture LoadTexture(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"No texture at '{path}'.", path);

        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension is ".png" or ".jpg" or ".jpeg")
            return LoadTextureFromMemory(File.ReadAllBytes(path), path);

        var surface = SDL.SDL_LoadBMP(path);

        if (surface == IntPtr.Zero)
            throw new InvalidOperationException($"Couldn't load '{path}': {SDL.SDL_GetError()}");

        try
        {
            SDL.SDL_SetSurfaceBlendMode(surface, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
            return CreateTextureFromSurface(surface);
        }
        finally
        {
            SDL.SDL_FreeSurface(surface);
        }
    }

    public Texture LoadTextureFromMemory(ReadOnlySpan<byte> data, string name = "texture")
    {
        if (data.IsEmpty)
            throw new ArgumentException("Texture data is empty.", nameof(data));

        ImageResult? image;

        try
        {
            image = ImageResult.FromMemory(data.ToArray(), ColorComponents.RedGreenBlueAlpha);
        }
        catch (Exception error)
        {
            throw new InvalidOperationException($"Couldn't decode '{name}': {error.Message}", error);
        }

        if (image is null || image.Data is null || image.Width <= 0 || image.Height <= 0)
            throw new InvalidOperationException($"Couldn't decode '{name}'.");

        return CreateTextureFromPixels(image.Data, image.Width, image.Height, name);
    }

    // raw RGBA pixels, top row first. used by PNG decoding and generated fonts
    public Texture CreateTextureFromPixels(byte[] rgba, int width, int height, string name = "texture")
    {
        ArgumentNullException.ThrowIfNull(rgba);
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        if (rgba.Length != width * height * 4)
            throw new ArgumentException("Pixel data must be width * height * 4 bytes of RGBA.", nameof(rgba));

        var pinned = GCHandle.Alloc(rgba, GCHandleType.Pinned);

        try
        {
            var surface = SDL.SDL_CreateRGBSurfaceFrom(
                pinned.AddrOfPinnedObject(),
                width,
                height,
                32,
                width * 4,
                0x000000FF,
                0x0000FF00,
                0x00FF0000,
                0xFF000000);

            if (surface == IntPtr.Zero)
                throw new InvalidOperationException($"Couldn't create a surface for '{name}': {SDL.SDL_GetError()}");

            try
            {
                SDL.SDL_SetSurfaceBlendMode(surface, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
                return CreateTextureFromSurface(surface);
            }
            finally
            {
                SDL.SDL_FreeSurface(surface);
            }
        }
        finally
        {
            pinned.Free();
        }
    }

    // grabs the current framebuffer and writes a PNG. hand it a screenshot
    // path from a debug key or a demo flag.
    public void SaveScreenshot(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (SDL.SDL_GetRendererOutputSize(_rendererHandle, out var width, out var height) != 0)
            throw new InvalidOperationException($"Couldn't read renderer size: {SDL.SDL_GetError()}");

        if (width <= 0 || height <= 0)
            throw new InvalidOperationException("The renderer has no pixels to save.");

        var stride = width * 4;
        var pixels = new byte[stride * height];
        var rectangle = new SDL.SDL_Rect { x = 0, y = 0, w = width, h = height };
        var pinned = GCHandle.Alloc(pixels, GCHandleType.Pinned);

        try
        {
            var result = SDL.SDL_RenderReadPixels(
                _rendererHandle,
                ref rectangle,
                SDL.SDL_PIXELFORMAT_ABGR8888,
                pinned.AddrOfPinnedObject(),
                stride);

            if (result != 0)
                throw new InvalidOperationException($"Couldn't read pixels: {SDL.SDL_GetError()}");
        }
        finally
        {
            pinned.Free();
        }

        PngWriter.Write(path, width, height, pixels);
    }

    internal Texture CreateTextureFromSurface(IntPtr surface)
    {
        var handle = SDL.SDL_CreateTextureFromSurface(_rendererHandle, surface);

        if (handle == IntPtr.Zero)
            throw new InvalidOperationException($"Couldn't create a texture: {SDL.SDL_GetError()}");

        SDL.SDL_SetTextureBlendMode(handle, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
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
