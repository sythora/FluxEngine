# FluxRunner story and dialogue

FluxRunner is a short, three-chapter platforming journey. Nova follows a
failing beacon across the rooftops to bring a darkened city back to life.
Chapters are separated by route milestones; reaching one saves a checkpoint
and pauses for a dialogue page. Falling or hitting a hazard returns Nova to
the latest checkpoint. The final milestone plays the ending.

![Story dialogue screen](images/story.png)

## Controls

- **A/D** or **Left/Right**: move
- **Space/Up** or gamepad **A**: jump, begin the story, and advance dialogue
- **Escape/Start**: pause
- **O/Y**: settings from the start or pause screen
- **R/Space** or gamepad **A** after a fall: respawn at the last checkpoint

The story is kept in `demos/FluxEngine.Demo/StoryCampaign.cs`. It contains the
opening pages, chapter titles and dialogue, and ending pages. Edit those
strings to change the narrative, or add a chapter to `Chapters` to extend the
route. `ChapterLength` controls how far the player travels before each
checkpoint and chapter transition.

## Dialogue UI

The dialogue box uses the built-in Press Start 2P font for titles and
Silkscreen for readable body copy. Text wraps to the panel width and each
dialogue page waits for a jump/confirm press:

```csharp
var titleFont = BuiltInFonts.Load(engine.Renderer, BuiltInFont.PressStart2P);
var bodyFont = BuiltInFonts.Load(engine.Renderer, BuiltInFont.Silkscreen);
var ui = new GameUI(engine.Input.Actions, titleFont, bodyFont, engine.Settings);

ui.ShowDialogue("A SIGNAL IN THE DARK",
[
    "The city's lights went out one by one.",
    "Nova follows a trail of lanterns toward the river.",
]);

if (engine.Input.Actions.WasPressed("jump") && ui.AdvanceDialogue())
{
    // The final page has finished; return to gameplay.
}
```

Both bundled fonts contain printable ASCII characters. Keep dialogue text in
that range, or load a BMFont atlas that includes the characters you need.
See [Fonts.md](Fonts.md) for additional font and atlas guidance.

## Current story text

### Prologue

> The city's lights went out one by one. At the edge of the rooftops, Nova
> found a signal still blinking.
>
> The old beacon can wake the city, but its path is broken. Follow the signal,
> cross the dark, and bring everyone home.

### The Quiet Rooftops

> The rooftops used to be loud after sunset. Nova follows a trail of lanterns
> toward the river.
>
> A note is tied beneath one light: "If you can see this, the beacon is still
> listening."

### The Sunken District

> Rain has filled the old streets below. Far across the water, a second signal
> answers.
>
> Nova remembers the keeper's promise: no one gets left behind while the light
> is on.

### The Last Beacon

> The final climb is steep, and the signal is almost gone. Nova keeps moving
> anyway.
>
> At the summit, the beacon catches. One window lights up, then another, then
> the whole city.

### Ending

> By morning, the rooftops are bright again. People follow the lights home,
> carrying the signal farther than Nova ever could.
>
> The city is awake. The next story belongs to everyone who found their way
> back.
