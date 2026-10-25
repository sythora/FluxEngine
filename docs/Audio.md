# Audio

Loading WAV files and playing them. One sound at a time, queued up. No fancy
mixing, no 3D audio, just bangs and bloops.

## Loading

```csharp
var jumpSound = engine.Audio.LoadSound("audio/jump.wav");
```

It only loads when you first ask for it. If SDL can't open an audio device
everything just stays quiet instead of crashing.

## Playing

```csharp
engine.Audio.Play(jumpSound, volume: 0.8f, loop: false);
```

- `volume` is 0 to 1
- `loop` makes it repeat forever until you call `StopAll`

```csharp
engine.Audio.StopAll();
engine.Audio.SetPaused(true); // pause everything
engine.Audio.SetPaused(false);
```

## Formats

8-bit, 16-bit, 32-bit PCM and 32-bit float WAVs work. Anything else gets
skipped. If your file won't load, convert it to a plain 16-bit WAV.

## Still missing

- No MP3 or OGG
- No sound mixing, sounds play one after another instead of on top of each
  other
- No spatial audio
