# Third-Party Notices

FluxEngine bundles the following third-party assets and libraries. License
terms for redistributable assets are included here or next to those assets.

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
- Runtime: SDL2 2.0.20, Windows x64, in
  `src/FluxEngine.Platform/runtimes/win-x64/native/SDL2.dll`
- Source: https://www.libsdl.org/ and https://github.com/flibitijibibo/SDL2-CS

SDL2 is distributed under the zlib license:

> This software is provided 'as-is', without any express or implied
> warranty. In no event will the authors be held liable for any damages
> arising from the use of this software.
>
> Permission is granted to anyone to use this software for any purpose,
> including commercial applications, and to alter it and redistribute it
> freely, subject to the following restrictions:
>
> 1. The origin of this software must not be misrepresented; you must not
>    claim that you wrote the original software. If you use this software
>    in a product, an acknowledgment in the product documentation would be
>    appreciated but is not required.
> 2. Altered source versions must be plainly marked as such, and must not be
>    misrepresented as being the original software.
> 3. This notice may not be removed or altered from any source distribution.

The bundled runtime's version metadata identifies SDL 2.0.20. FluxEngine
copies the Windows x64 runtime to build and publish output automatically.
Other platforms still require SDL2 from their system or distribution.

## OFL summary

The SIL Open Font License allows the fonts to be used, studied, modified, and
redistributed freely as long as they are not sold by themselves. The fonts and
derivatives may be bundled, embedded, and redistributed with software. The
reserved font names "Press Start 2P" and "Silkscreen" may not be used by
modified versions without permission. The full license text is authoritative;
see the `*-OFL.txt` files.
