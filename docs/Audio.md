# Audio

Load WAV files and play overlapping sound effects and music. The engine mixes
active voices on SDL's audio callback, so a coin pickup can play over a jump
or background track.

## Loading

```csharp
var jumpSound = engine.Audio.LoadSound("audio/jump.wav");
```

Loads are cached by normalized full path. Repeating the same load returns the
same `Sound`; use `UnloadSound(sound)` or `ClearSounds()` when you want to free
cached sample memory. If SDL can't open an audio device, playback stays quiet
instead of crashing.

## Playing

```csharp
engine.Audio.Play(jumpSound, volume: 0.8f, loop: false);
```

- `volume` is 0 to 1
- `loop` makes it repeat until you call `StopAll`
- `bus` is `AudioBus.Sfx` by default; use `AudioBus.Music` for music playback

```csharp
engine.Audio.Play(music, volume: 0.8f, loop: true, bus: AudioBus.Music);
engine.Audio.MasterVolume = 0.8f;
engine.Audio.SfxVolume = 0.9f;
engine.Audio.MusicVolume = 0.5f;
engine.Audio.StopAll();
engine.Audio.SetPaused(true); // pause everything
engine.Audio.SetPaused(false);
```

Master, SFX, and music volume are multiplied into the output mix and can be
changed while a sound is playing. Each call to `Play` starts a new voice
immediately, and voices from both buses are mixed together. `ActiveVoiceCount`
is useful for a lightweight audio debug overlay. `SetPaused` also remembers
the requested state if called before the audio device is opened.

## Formats

8-bit, 16-bit, and 32-bit PCM plus 32-bit float WAVs work. Sounds with
different sample rates or channel counts are converted to match the first
opened audio device. WAV parsing works without an SDL audio device; audio
output itself still requires SDL.

The root `assets/` folder includes original `jump.wav`, `land.wav`, `coin.wav`,
`die.wav`, and `music.wav` samples used by FluxRunner. They are small mono
16-bit PCM examples you can replace with your own sounds.

## Still missing

- No MP3 or OGG
- No spatial audio or per-voice positioning
- No fades, filters, or streaming for long music tracks
