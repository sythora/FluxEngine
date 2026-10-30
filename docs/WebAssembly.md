# Browser WebAssembly

FluxEngine's browser host uses the .NET browser WebAssembly runtime to run the
existing C# engine assembly. It does not replace the engine with JavaScript.
The WebAssembly AOT toolchain uses Emscripten internally; Emscripten does not
compile the managed FluxEngine DLL directly.

## Run locally

Install the .NET 10 SDK and the WebAssembly build tools workload:

```text
dotnet workload install wasm-tools
dotnet run --project web/FluxEngine.Web
```

The host runs locally at the URL printed by `dotnet run`. To publish an
optimized WebAssembly build:

```text
dotnet publish web/FluxEngine.Web -c Release
```

## Browser game

The host runs FluxRunner: Afterlight using the engine's C# `Scene`,
`Rigidbody2D`, one-way platform colliders, `InputActions`, `Camera`, and
`SceneLoop`. The browser edition has the same three-chapter story and adds
procedural rooftops, double-jump movement, hazards, signal and memory
collectibles, lantern checkpoints, achievements, lives, pause/settings/story
screens, synthesized Web Audio effects, browser-local saves, and touch controls.
The linked `StoryCampaign.cs` keeps the chapter dialogue and completion rules
shared with the desktop game.

The JavaScript bridge is limited to browser integration: animation-frame
scheduling, keyboard/pointer/touch events, Canvas 2D draw commands, Web Audio,
and `localStorage`. Scene behavior, player movement, collision handling,
campaign progression, and game state remain in C#.

The browser renderer supports clearing, rectangles, outlines, lines, circles,
polygons, gradients, and text. It does not yet support SDL-backed texture
loading/drawing, native audio, lighting, or framebuffer screenshots.
Unsupported renderer operations throw `PlatformNotSupportedException` instead
of silently falling back to native SDL. Native desktop and editor projects
continue to use SDL2.

The browser edition currently targets the WebAssembly project only; publish
output is written to `build/FluxEngine.Web/Release/net10.0/publish/`.
