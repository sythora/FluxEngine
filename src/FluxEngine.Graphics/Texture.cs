using SDL2;
using FluxEngine.Mathematics;

namespace FluxEngine.Graphics;

public sealed class Texture : IDisposable
{
    internal IntPtr Handle { get; private set; }

    public int Width { get; }
    public int Height { get; }

    public Rectangle Bounds => new(0, 0, Width, Height);

    internal Texture(IntPtr handle, int width, int height)
    {
        Handle = handle;
        Width = width;
        Height = height;
    }

    public void Dispose()
    {
        if (Handle == IntPtr.Zero)
            return;

        SDL.SDL_DestroyTexture(Handle);
        Handle = IntPtr.Zero;
    }
}
