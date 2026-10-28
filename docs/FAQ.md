# FAQ

## The window never opens

You probably don't have SDL2.dll somewhere the app can find it. Put it next to
the exe or add its folder to PATH. On Windows the build output goes to
`build/<project>/Debug/net10.0/` (for example `build/FluxEngine.Demo/Debug/net10.0/`).

## I want PNG images

SDL core doesn't do PNG, only BMP. You can convert your art to BMP or add
SDL_image yourself. It's annoying as hell but that's how it is.

## Can I use this for a real game?

Sure, it's just missing text, PNGs, sound mixing, and a bunch of other stuff.
Add what you need.

## Why is it called FluxEngine

Because it sounded cool. That's it.

## Something is broken

Check `docs/PROJECT_STRUCTURE.md` to see what file does what. If you still
can't figure it out, the code is pretty small. Just read it.
