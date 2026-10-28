# FAQ

## The window never opens

You probably don't have SDL2.dll somewhere the app can find it. Put it next to
the exe or add its folder to PATH. On Windows the build output goes to
`build/<project>/Debug/net10.0/` (for example `build/FluxEngine.Demo/Debug/net10.0/`).

## I want PNG images

Use them. `Renderer.LoadTexture` decodes PNG and JPEG with StbImageSharp, and
BMP still works through SDL. See [Graphics.md](Graphics.md).

## Can I use this for a real game?

Sure. It has PNG textures, bitmap fonts, a WAV mixer, physics, and an editor.
Add whatever else your game needs.

## Why is it called FluxEngine

Because it sounded cool. That's it.

## Something is broken

Check `docs/PROJECT_STRUCTURE.md` to see what file does what. If you still
can't figure it out, the code is pretty small. Just read it.
