# Settings

`EngineSettings` stores JSON settings for the engine and demo. An `Engine`
loads `settings.json` from the process working directory when it is created;
missing or invalid files use the defaults. Saving creates the parent directory
if needed.

## Demo controls

Open the settings screen from the start screen or pause screen with **O** or
gamepad **Y**. Use **Up/Down** to select an option, **Left/Right** to adjust,
**Enter** to toggle/increase/select, and **Escape** or gamepad **B** to return
without saving. [Preview the settings screen](images/settings.png). The screen contains:

| Option | Effect |
| --- | --- |
| Lighting, vignette, god rays, atmospheric particles, fog, screen shake | Visual effects update immediately |
| Master, SFX, and music volume | Adjust the audio mix live |
| VSync | Toggle SDL renderer synchronization |
| Frame-rate limit | Cycle 30, 60, 120 FPS, or unlimited |
| Save and return | Write settings and leave the screen |

The frame-rate limit is used only when VSync is off. Settings can also be
saved at any time with **F5**. The file is written as `settings.json` in the
current working directory, so run the demo from the same directory to reuse
that file on the next launch. Pressing Escape/B leaves changes active for the
current run but does not save them.

## Engine API

The settings object can be inspected or replaced by a game:

```csharp
using FluxEngine.Core;

using var engine = new Engine("my game", 960, 640);

engine.Settings.EnableLighting = false;
engine.Settings.Vsync = false;
engine.Settings.TargetFps = 120;
engine.Settings.NotifyChanged();
```

Save to the default path or choose another path:

```csharp
if (!engine.Settings.Save())
    throw new IOException("Could not save settings.");

var settings = EngineSettings.Load("config/settings.json");
engine.Settings = settings;
```

`Save` returns `false` and logs a warning if writing fails. `Changed` is an
optional notification event; call `NotifyChanged()` after applying a group of
changes when consumers need to react.

## Available values and defaults

| Property | Default |
| --- | --- |
| `EnableLighting` | `true` |
| `EnableVignette` | `true` |
| `EnableGodRays` | `true` |
| `EnableAtmosphericParticles` | `true` |
| `EnableFog` | `true` |
| `EnableScreenShake` | `true` |
| `Vsync` | `true` |
| `TargetFps` | `60` |
| `MasterVolume` | `1.0` |
| `SfxVolume` | `1.0` |
| `MusicVolume` | `0.8` |

The demo applies the visual-effect options. Display and audio values are
applied to the SDL renderer, engine frame loop, and audio mixer. `TargetFps` is
bounded from 0 to 1000, where 0 means uncapped. Volume values are bounded
from 0 to 1.

The engine applies VSync and audio changes when `NotifyChanged()` is called
after mutating settings. The demo settings screen does this automatically.
