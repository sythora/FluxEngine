# FluxEngine Roadmap

This roadmap turns the proposed feature list into an incremental plan for FluxEngine, a .NET 10 / SDL2 2D engine. The goal is a dependable, practical pixel-art game workflow-not a general-purpose commercial engine. Phases are ordered by prerequisites and value; later items are targets, not claims that they are already implemented.

## Current milestone: stability pass

- [x] Make the scene editor serialize its current editable objects on Save, Save As, and close, rather than stale data loaded before edits.
- [x] Add grounded jump, one mid-air jump, coyote grace, buffered jump presses, and variable jump height to FluxRunner.
- [x] Generate collectible coins and spike hazards in the demo; update score, distance, time, and best-distance HUD data.
- [x] Add regression tests for scene edit/save/reopen consistency, collision queries, entity/component lifecycle, timers, level loading, and game-manager behavior.
- [x] Replace the Python input test with the C# regression suite and include both test projects in the solution.
- [x] Add PNG/JPEG loading with alpha, screenshot PNG output, built-in OFL bitmap fonts, and BMFont text-map loading.
- [ ] Verify gameplay in a running demo, including every advertised control and collectible/hazard behavior.

## Phase 1 - Reliable content and level foundations

1. **Automated engine tests and lifecycle/resource correctness**
   - Extend the dependency-free regression test project to cover collisions, scene serialization round trips, entity/component start and destruction, timers, level loading, and game manager behavior.
   - Fix lifecycle/resource ownership issues discovered by the tests; avoid accumulating stale colliders or native resources.
2. **Image and asset pipeline**
   - PNG/JPEG loading with alpha and PNG screenshot output are in place. Next: evaluate QOI/TGA only if they add meaningful value.
   - Add named asset references, preloading/validation, useful missing-asset errors, predictable texture disposal, and optional source-change hot reload.
3. **Tiled JSON tilemaps**
   - Import Tiled JSON maps rather than first building a large custom tile editor.
   - Support tilesets, tile layers, object layers, visibility/order, and map/tile metadata needed by the engine.
   - Render background, foreground, collision, and decorative layers; build collision geometry from solid tiles.
   - Start with rule-based terrain adjacency/autotiling that can consume Tiled terrain metadata; keep painting and editing in Tiled initially.

## Phase 2 - Animation and platformer gameplay

1. **Animation controller**
   - Add named clips (for example Idle, Run, Jump, Fall, Attack, Death), frame durations, playback speed, and loop/once modes around the existing animated sprite renderer.
   - Allow state-based transitions, frame events, and optional animation locking, with clean pixel-art frame changes and no blending by default.
2. **Collision and queries**
   - Add collision layers/masks and Enter/Stay/Exit contact events for collision and trigger pairs.
   - Add raycasts and overlap queries; improve corner handling and moving-platform passenger transport.
   - Add platform-controller support for jump buffering, coyote time, variable-height jumps, and dropping through one-way platforms. Evaluate slopes separately after box contacts are reliable.
3. **Audio mixer**
   - Replace serialized playback with simultaneous voices; add master/music/SFX buses, per-sound volume, panning, pause/resume/stop, and fades.
   - Cache assets and make ownership/disposal explicit; support looping and streaming music where the SDL2-compatible backend permits it.
4. **Gamepad and remappable input**
   - Extend named actions with gamepad buttons, sticks, and triggers; support dead zones, multiple device bindings, disconnect handling, runtime rebinding, and saved profiles.

## Phase 3 - Editor and developer experience

1. **Choose the editor platform before replacing the current one.** The current editor is Windows Forms. A cross-platform replacement is a separate architectural decision and should be scoped before implementation; preserve the scene format and test coverage during any migration.
2. **Make the serialized scene the editor's authoritative model** and keep import/export round trips covered by tests.
3. **Editor workflow:** undo/redo, hierarchy, multi-selection, transform gizmos, component/transform inspector, duplicate/copy/paste, snapping, shortcuts, and safe unsaved-change handling.
4. **Play-in-editor:** run a scene for testing without manual demo launch and keep runtime changes isolated from saved edit state unless explicitly applied.
5. **Later editor tools:** asset browser and reusable entity prefabs after the core scene workflow is reliable.
6. **Debug overlay:** FPS/frame and update time, entity/component/particle/voice counts, collision and trigger shapes, world bounds, camera state, input state, recent warnings, exception pause, and component inspection.

## Phase 4 - Reusable game systems

- Versioned save files for progress, checkpoints, settings, and high scores.
- Scene transitions with fades, loading, additive/background scenes, and persistent entities.
- Tweening and sequences for transforms, colors, camera, and UI.
- A 2D UI system with anchors, layout, controls, menus, and gamepad navigation.
- Reusable prefabs with clear override/update behavior.
- Performance tooling for frame-time history, allocations, resource usage, and reproducible benchmark scenes.

## Phase 5 - Rendering and polish

- Pixel-perfect camera support: virtual resolution, integer scaling, pixel alignment, and resize behavior.
- Investigate lighting and post-processing against SDL_Renderer capabilities first. If the desired effects cannot be supported cleanly, design an explicit GPU-rendering extension rather than layering fragile effects into the existing path.

## Not currently planned

3D rendering, replacing the custom platformer physics with a general rigid-body engine, networking, and visual scripting are intentionally out of scope until the 2D workflow above is working well.

## Status notes

FluxRunner's current demo has gained double jump, coyote grace, buffered/variable-height jumps, generated coin and hazard entities, and connected HUD values. It now loads PNG textures generated at runtime and renders its HUD with the embedded Press Start 2P and Silkscreen fonts. The scene editor captures live edits into its serialized scene when saving, with regression coverage for scene save/reopen/save and core systems. Runtime gameplay verification remains. The editor is still Windows Forms; the roadmap describes its eventual cross-platform replacement, but that migration has not started.
