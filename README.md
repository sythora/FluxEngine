# FluxEngine - Infinite Platformer Demo

A 2D infinite-scrolling platformer built with FluxEngine, a C# game engine on top of SDL2.

## Build

```text
dotnet build FluxEngine.slnx
```

## Run

```text
dotnet run --project demos/FluxEngine.Demo
```

Requires `SDL2.dll` next to the exe at `artifacts/bin/Debug/net10.0/SDL2.dll`.
Download SDL2 from https://www.libsdl.org/ (get `SDL2-devel-2.0.20-VC.zip`, take `x64/SDL2.dll`).

## Controls

- A / D or Left / Right arrows: move
- Space / Up: jump (double jump available)
- Escape: pause / resume
- R: restart after game over

## Features

- Procedural infinite platform generation with increasing difficulty
- Double jump and coyote time for responsive controls
- Collectible coins with score
- Spike hazards and fall death
- Parallax background with clouds and distant hills
- Particle effects for jumps, landings, and deaths
- HUD showing score, distance, survival time, and best distance
- Start, pause, and game-over screens with pixel font
- Background music and sound effects (WAV)
- Camera smooth follow

## Project Structure

```
FluxEngine.slnx
Directory.Build.props
src/                    Engine source (math, core, graphics, physics, audio, input, content, platform)
editor/                 Standalone WinForms scene editor
demos/FluxEngine.Demo/  Infinite platformer demo
tests/                  Automated tests
docs/                   Documentation
```

## Architecture

- **Engine** (`Engine.cs`): Main loop with fixed timestep physics and variable timestep rendering.
- **Scene/Entity/Component**: ECS-style architecture with components for movement, rendering, physics.
- **Renderer**: SDL2-based drawing with shapes, sprites, and textures (BMP only).
- **Audio**: WAV loading and mixing via SDL audio callback.
- **Physics**: AABB collision with moving platforms and one-way platforms.
- **PlatformGenerator**: Procedural platform spawning with reachability validation.

## Audio Assets

Place WAV files in `demos/FluxEngine.Demo/assets/`:
- `jump.wav` - jump sound
- `land.wav` - landing sound
- `coin.wav` - coin pickup sound
- `die.wav` - death sound
- `music.wav` - background music (loops)

Missing audio files are skipped gracefully with a warning.

## Textures

All textures are generated procedurally as BMP files at runtime. No external art files required.
