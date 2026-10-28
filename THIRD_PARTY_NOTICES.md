# Third-Party Notices

FluxEngine bundles the following third-party assets and libraries. Full license
texts live next to the assets in this repository.

## StbImageSharp

- Package: `StbImageSharp` 2.30.16
- Used for: PNG/JPEG decoding in `Renderer.LoadTexture` and
  `Renderer.LoadTextureFromMemory`
- License: The Unlicense (public domain)
- Source: https://github.com/StbSharp/StbImageSharp

`StbImageSharp` wraps the public-domain `stb_image` decoder by Sean Barrett.
The package's `Unlicense` declaration is available in the NuGet package
metadata.

## Press Start 2P

- Copyright 2012 The Press Start 2P Project Authors (cody@zone38.net),
  with Reserved Font Name "Press Start 2P".
- License: SIL Open Font License, Version 1.1
- Source: https://github.com/google/fonts/tree/main/ofl/pressstart2p
- Bundled files:
  - `src/FluxEngine.Content/Fonts/PressStart2P-8.fnt`
  - `src/FluxEngine.Content/Fonts/PressStart2P-8.png`
  - `src/FluxEngine.Content/Fonts/PressStart2P-OFL.txt`

The bundled files are a rasterized 8px bitmap atlas and BMFont text map
generated from the original font. The OFL license text is included in full in
`PressStart2P-OFL.txt` and applies to the font and its derivatives.

## Silkscreen

- Copyright 2001 The Silkscreen Project Authors
  (https://github.com/googlefonts/silkscreen)
- License: SIL Open Font License, Version 1.1
- Source: https://github.com/google/fonts/tree/main/ofl/silkscreen
- Bundled files:
  - `src/FluxEngine.Content/Fonts/Silkscreen-8.fnt`
  - `src/FluxEngine.Content/Fonts/Silkscreen-8.png`
  - `src/FluxEngine.Content/Fonts/Silkscreen-OFL.txt`

The bundled files are a rasterized 8px bitmap atlas and BMFont text map
generated from the original font. The OFL license text is included in full in
`Silkscreen-OFL.txt` and applies to the font and its derivatives.

## SDL2 and SDL2-CS

- Project: Simple DirectMedia Layer 2 (zlib license)
- Binding: `SDL2-CS` (zlib license)
- Source: https://www.libsdl.org/ and https://github.com/flibitijibibo/SDL2-CS

FluxEngine does not redistribute SDL2 binaries. See the SDL2 license for the
copy you place next to the executable.

## OFL summary

The SIL Open Font License allows the fonts to be used, studied, modified, and
redistributed freely as long as they are not sold by themselves. The fonts and
derivatives may be bundled, embedded, and redistributed with software. The
reserved font names "Press Start 2P" and "Silkscreen" may not be used by
modified versions without permission. The full license text is authoritative;
see the `*-OFL.txt` files.
