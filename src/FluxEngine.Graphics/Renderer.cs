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
    private readonly List<BrowserDrawCommand>? _browserCommands;
    private readonly int _browserWidth;
    private readonly int _browserHeight;
    private Texture? _radialLightTexture;
    private Texture? _vignetteTexture;
    private IntPtr _lightmapTexture;
    private int _lightmapWidth;
    private int _lightmapHeight;

    internal Renderer(IntPtr windowHandle, IntPtr rendererHandle)
    {
        _windowHandle = windowHandle;
        _rendererHandle = rendererHandle;

        // alpha blending for all draw primitives (rects, lines, etc.)
        SDL.SDL_SetRenderDrawBlendMode(_rendererHandle, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
    }

    private Renderer(int browserWidth, int browserHeight)
    {
        if (browserWidth <= 0 || browserHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(browserWidth), "Browser renderer dimensions must be positive.");

        _browserWidth = browserWidth;
        _browserHeight = browserHeight;
        _browserCommands = [];
    }

    public static Renderer CreateBrowser(int width, int height) => new(width, height);

    public bool IsBrowserBackend => _browserCommands is not null;

    public IReadOnlyList<BrowserDrawCommand> BrowserCommands
        => _browserCommands ?? throw new InvalidOperationException("This renderer is not using the browser backend.");

    public void BeginBrowserFrame(Color backgroundColor)
    {
        if (_browserCommands is null)
            throw new InvalidOperationException("This renderer is not using the browser backend.");

        _browserCommands.Clear();
        _browserCommands.Add(new BrowserDrawCommand("clear", [], backgroundColor, Color.Transparent));
    }

    public void DrawBrowserRectangle(Rectangle rectangle, Color color, bool outline = false, bool screenSpace = true)
        => AddBrowserCommand(
            outline ? "rectangle-outline" : "rectangle",
            [rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height],
            color,
            Color.Transparent,
            screenSpace: screenSpace);

    public void DrawBrowserCircle(Vector2 center, float radius, Color color, bool outline = false, bool screenSpace = true)
        => AddBrowserCommand(
            outline ? "circle-outline" : "circle",
            [center.X, center.Y, radius],
            color,
            Color.Transparent,
            screenSpace: screenSpace);

    public void DrawBrowserLine(Vector2 from, Vector2 to, Color color, bool screenSpace = true)
        => AddBrowserCommand("line", [from.X, from.Y, to.X, to.Y], color, Color.Transparent, screenSpace: screenSpace);

    public void DrawBrowserPolygon(IReadOnlyList<Vector2> points, Color color, bool filled = false, bool screenSpace = true)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
            return;

        var coordinates = new float[points.Count * 2];
        for (var index = 0; index < points.Count; index++)
        {
            coordinates[index * 2] = points[index].X;
            coordinates[index * 2 + 1] = points[index].Y;
        }

        AddBrowserCommand(filled ? "filled-polygon" : "polygon", coordinates, color, Color.Transparent, screenSpace: screenSpace);
    }

    public void DrawBrowserText(
        string text,
        Vector2 position,
        Color color,
        float size = 16,
        bool centered = false,
        bool bold = false,
        bool screenSpace = true)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!float.IsFinite(size) || size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Text size must be positive and finite.");

        AddBrowserCommand(
            "text",
            [position.X, position.Y, size, centered ? 1 : 0, bold ? 1 : 0],
            color,
            Color.Transparent,
            text,
            screenSpace: screenSpace);
    }

    // scene sets this every frame
    public Camera? Camera { get; set; }

    private float Zoom => Camera is { Zoom: > 0 } camera ? camera.Zoom : 1f;

    public void Clear(Color color)
    {
        if (_browserCommands is not null)
        {
            BeginBrowserFrame(color);
            return;
        }

        SDL.SDL_SetRenderDrawColor(_rendererHandle, color.R, color.G, color.B, color.A);
        SDL.SDL_RenderClear(_rendererHandle);
    }

    public void DrawRectangle(Rectangle rectangle, Color color)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
            return;

        if (_browserCommands is not null)
        {
            _browserCommands.Add(new BrowserDrawCommand(
                "rectangle", [rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height], color, Color.Transparent));
            return;
        }

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
        if (_browserCommands is not null)
        {
            _browserCommands.Add(new BrowserDrawCommand(
                "rectangle-outline", [rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height], color, Color.Transparent));
            return;
        }

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
        if (_browserCommands is not null)
        {
            _browserCommands.Add(new BrowserDrawCommand(
                "line", [from.X, from.Y, to.X, to.Y], color, Color.Transparent));
            return;
        }

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
        if (_browserCommands is not null)
        {
            if (radius > 0)
                _browserCommands.Add(new BrowserDrawCommand(
                    "circle", [center.X, center.Y, radius], color, Color.Transparent));
            return;
        }

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
        if (_browserCommands is not null)
        {
            if (radius > 0)
                _browserCommands.Add(new BrowserDrawCommand(
                    "circle-outline", [center.X, center.Y, radius, segments], color, Color.Transparent));
            return;
        }

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

    // four corners filled scanline by scanline while the color fades from the
    // top edge to the bottom edge. used by soft god rays and light shafts.
    // assumes a convex quad ordered (top-left, top-right, bottom-right, bottom-left)
    public void DrawQuadGradient(
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomLeft,
        Vector2 bottomRight,
        Color topColor,
        Color bottomColor)
    {
        if (_browserCommands is not null)
        {
            _browserCommands.Add(new BrowserDrawCommand(
                "quad-gradient",
                [topLeft.X, topLeft.Y, topRight.X, topRight.Y, bottomLeft.X, bottomLeft.Y, bottomRight.X, bottomRight.Y],
                topColor,
                bottomColor));
            return;
        }

        var tl = ToScreen(topLeft);
        var tr = ToScreen(topRight);
        var br = ToScreen(bottomRight);
        var bl = ToScreen(bottomLeft);

        var minY = (int)MathF.Ceiling(Math.Min(Math.Min(tl.Y, tr.Y), Math.Min(bl.Y, br.Y)));
        var maxY = (int)MathF.Floor(Math.Max(Math.Max(tl.Y, tr.Y), Math.Max(bl.Y, br.Y)));

        var sides = new (Vector2 a, Vector2 b, Color ca, Color cb)[]
        {
            (tl, tr, topColor, topColor),       // top edge
            (tr, br, topColor, bottomColor),    // right edge
            (br, bl, bottomColor, bottomColor), // bottom edge (rarely hit at y>
            (bl, tl, bottomColor, topColor),    // left edge
        };

        for (var y = minY; y <= maxY; y++)
        {
            var scanY = y + 0.5f;
            float? leftX = null, rightX = null;
            Color? leftColor = null, rightColor = null;

            foreach (var (a, b, ca, cb) in sides)
            {
                if (a.Y == b.Y)
                    continue;

                var t = (scanY - a.Y) / (b.Y - a.Y);
                if (t < 0f || t > 1f)
                    continue;

                var x = a.X + (b.X - a.X) * t;
                var color = Color.Lerp(ca, cb, t);

                if (leftX is null || x < leftX)
                {
                    leftX = x;
                    leftColor = color;
                }

                if (rightX is null || x > rightX)
                {
                    rightX = x;
                    rightColor = color;
                }
            }

            if (leftX is null || rightX is null)
                continue;

            var spanStart = leftX.Value;
            var spanEnd = rightX.Value;
            var startColor = leftColor!.Value;
            var endColor = rightColor!.Value;

            // fill the full scanline row in fixed slices so wide beams stay smooth
            const int slices = 6;

            if (spanEnd <= spanStart)
                continue;

            var width = spanEnd - spanStart;
            var sliceWidth = width / slices;
            for (var s = 0; s < slices; s++)
            {
                var t0 = s / (float)slices;
                var t1 = s < slices - 1 ? (s + 1) / (float)slices : 1f;
                var x0 = spanStart + width * t0;
                var x1 = spanStart + width * t1;
                var color = Color.Lerp(startColor, endColor, (t0 + t1) * 0.5f);
                var rect = new SDL.SDL_Rect
                {
                    x = (int)x0,
                    y = y,
                    w = (int)MathF.Ceiling(x1 - x0) + 1,
                    h = 1,
                };

                SetColor(color);
                SDL.SDL_RenderFillRect(_rendererHandle, ref rect);
            }
        }
    }

    // closed shape from a list of points, lines only, no fill
    public void DrawPolygon(IReadOnlyList<Vector2> points, Color color)
    {
        if (points.Count < 2)
            return;

        if (_browserCommands is not null)
        {
            var coordinates = new float[points.Count * 2];
            for (var index = 0; index < points.Count; index++)
            {
                coordinates[index * 2] = points[index].X;
                coordinates[index * 2 + 1] = points[index].Y;
            }

            _browserCommands.Add(new BrowserDrawCommand("polygon", coordinates, color, Color.Transparent));
            return;
        }

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

        if (_browserCommands is not null)
            throw new PlatformNotSupportedException("Browser renderer texture drawing is not implemented yet.");

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

        if (_browserCommands is not null)
            throw new PlatformNotSupportedException("Browser renderer sprite drawing is not implemented yet.");

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
        EnsureNativeBackend("Texture loading");

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
        EnsureNativeBackend("Texture loading");

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
        EnsureNativeBackend("Texture creation");

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
        EnsureNativeBackend("Saving screenshots");

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

    public bool SupportsLighting =>
        _browserCommands is null &&
        _rendererHandle != IntPtr.Zero &&
        SDL.SDL_RenderTargetSupported(_rendererHandle) == SDL.SDL_bool.SDL_TRUE;

    private Texture GetOrCreateRadialLightTexture()
    {
        if (_radialLightTexture is not null)
            return _radialLightTexture;

        const int size = 128;
        var pixels = new byte[size * size * 4];
        var center = (size - 1) * 0.5f;
        var maxRadius = size * 0.5f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var dist = MathF.Sqrt(dx * dx + dy * dy);
                var t = Math.Clamp(dist / maxRadius, 0f, 1f);

                var falloff = t >= 1f ? 0f : (1f + MathF.Cos(t * MathF.PI)) * 0.5f;
                var alpha = (byte)(falloff * 255f);

                var index = (y * size + x) * 4;
                pixels[index + 0] = alpha;
                pixels[index + 1] = alpha;
                pixels[index + 2] = alpha;
                pixels[index + 3] = alpha;
            }
        }

        _radialLightTexture = CreateTextureFromPixels(pixels, size, size, "radial_light_mask");
        SDL.SDL_SetTextureBlendMode(_radialLightTexture.Handle, SDL.SDL_BlendMode.SDL_BLENDMODE_ADD);
        return _radialLightTexture;
    }

    private void EnsureLightmap(int width, int height)
    {
        if (_lightmapTexture != IntPtr.Zero && _lightmapWidth == width && _lightmapHeight == height)
            return;

        if (_lightmapTexture != IntPtr.Zero)
        {
            SDL.SDL_DestroyTexture(_lightmapTexture);
            _lightmapTexture = IntPtr.Zero;
        }

        _lightmapWidth = Math.Max(1, width);
        _lightmapHeight = Math.Max(1, height);

        _lightmapTexture = SDL.SDL_CreateTexture(
            _rendererHandle,
            SDL.SDL_PIXELFORMAT_RGBA8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET,
            _lightmapWidth,
            _lightmapHeight);

        if (_lightmapTexture != IntPtr.Zero)
            SDL.SDL_SetTextureBlendMode(_lightmapTexture, SDL.SDL_BlendMode.SDL_BLENDMODE_MOD);
    }

    public bool BeginLightmap(Color ambientColor)
    {
        if (!SupportsLighting)
            return false;

        SDL.SDL_GetWindowSize(_windowHandle, out var width, out var height);
        EnsureLightmap(width, height);

        if (_lightmapTexture == IntPtr.Zero)
            return false;

        if (SDL.SDL_SetRenderTarget(_rendererHandle, _lightmapTexture) != 0)
            return false;

        SDL.SDL_SetRenderDrawColor(_rendererHandle, ambientColor.R, ambientColor.G, ambientColor.B, 255);
        SDL.SDL_RenderClear(_rendererHandle);
        return true;
    }

    public void DrawLight(Vector2 worldPosition, float radius, Color color, float intensity = 1f)
    {
        if (_lightmapTexture == IntPtr.Zero || radius <= 0f || intensity <= 0f)
            return;

        var screen = ToScreen(worldPosition);
        var pixelRadius = radius * Zoom;
        if (pixelRadius <= 0f)
            return;

        var diameter = (int)(pixelRadius * 2);
        var rect = new SDL.SDL_Rect
        {
            x = (int)(screen.X - pixelRadius),
            y = (int)(screen.Y - pixelRadius),
            w = diameter,
            h = diameter,
        };

        var radial = GetOrCreateRadialLightTexture();
        var effectiveAlpha = (byte)Math.Clamp(color.A * intensity, 0, 255);

        SDL.SDL_SetTextureColorMod(radial.Handle, color.R, color.G, color.B);
        SDL.SDL_SetTextureAlphaMod(radial.Handle, effectiveAlpha);
        SDL.SDL_RenderCopy(_rendererHandle, radial.Handle, IntPtr.Zero, ref rect);
        SDL.SDL_SetTextureColorMod(radial.Handle, 255, 255, 255);
        SDL.SDL_SetTextureAlphaMod(radial.Handle, 255);
    }

    public void EndLightmap()
    {
        if (_lightmapTexture == IntPtr.Zero)
            return;

        SDL.SDL_SetRenderTarget(_rendererHandle, IntPtr.Zero);
        SDL.SDL_RenderCopy(_rendererHandle, _lightmapTexture, IntPtr.Zero, IntPtr.Zero);
    }

    private Texture GetOrCreateVignetteTexture()
    {
        if (_vignetteTexture is not null)
            return _vignetteTexture;

        const int size = 256;
        var pixels = new byte[size * size * 4];
        var center = (size - 1) * 0.5f;
        var maxDist = MathF.Sqrt(center * center + center * center);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var dist = MathF.Sqrt(dx * dx + dy * dy);
                var normalized = dist / maxDist;

                var t = Math.Clamp((normalized - 0.35f) / 0.65f, 0f, 1f);
                var alpha = (byte)(MathF.Pow(t, 1.8f) * 235f);

                var index = (y * size + x) * 4;
                pixels[index + 0] = 5;
                pixels[index + 1] = 8;
                pixels[index + 2] = 16;
                pixels[index + 3] = alpha;
            }
        }

        _vignetteTexture = CreateTextureFromPixels(pixels, size, size, "vignette_mask");
        return _vignetteTexture;
    }

    public void DrawVignette(float intensity = 0.85f)
    {
        if (_rendererHandle == IntPtr.Zero || intensity <= 0f)
            return;

        var vignette = GetOrCreateVignetteTexture();
        SDL.SDL_GetWindowSize(_windowHandle, out var width, out var height);

        var rect = new SDL.SDL_Rect { x = 0, y = 0, w = width, h = height };
        var alphaMod = (byte)Math.Clamp(intensity * 255f, 0, 255);

        SDL.SDL_SetTextureAlphaMod(vignette.Handle, alphaMod);
        SDL.SDL_RenderCopy(_rendererHandle, vignette.Handle, IntPtr.Zero, ref rect);
        SDL.SDL_SetTextureAlphaMod(vignette.Handle, 255);
    }

    internal void Dispose()
    {
        if (_lightmapTexture != IntPtr.Zero)
        {
            SDL.SDL_DestroyTexture(_lightmapTexture);
            _lightmapTexture = IntPtr.Zero;
        }

        _radialLightTexture?.Dispose();
        _radialLightTexture = null;

        _vignetteTexture?.Dispose();
        _vignetteTexture = null;
    }

    private Vector2 ToScreen(Vector2 world)
    {
        if (Camera is null)
            return world;

        var width = _browserWidth;
        var height = _browserHeight;
        if (_browserCommands is null)
            SDL.SDL_GetWindowSize(_windowHandle, out width, out height);
        return Camera.WorldToScreen(world, width, height);
    }

    private void EnsureNativeBackend(string operation)
    {
        if (_browserCommands is not null)
            throw new PlatformNotSupportedException($"{operation} is not available on the browser renderer yet.");
    }

    private void AddBrowserCommand(
        string type,
        float[] values,
        Color color,
        Color secondaryColor,
        string? text = null,
        bool screenSpace = false)
    {
        if (_browserCommands is null)
            throw new PlatformNotSupportedException("Browser-only drawing commands require the browser renderer.");

        _browserCommands.Add(new BrowserDrawCommand(type, values, color, secondaryColor, text, screenSpace));
    }

    private void SetColor(Color color)
        => SDL.SDL_SetRenderDrawColor(_rendererHandle, color.R, color.G, color.B, color.A);
}
