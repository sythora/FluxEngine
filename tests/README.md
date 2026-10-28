# Tests

Two console projects cover the engine and its platform layer:

```text
tests/
  FluxEngine.RegressionTests/  Automated engine regression suite
  FluxEngine.GamepadTest/      Interactive gamepad/controller check
```

## Regression tests

```text
dotnet run --project tests/FluxEngine.RegressionTests/FluxEngine.RegressionTests.csproj
```

The regression suite covers editor scene save/reopen/save consistency,
engine/editor scene format compatibility, box and circle collision queries,
deferred component/entity lifecycle, timers, level text loading/saving and
malformed data, and game-manager score/death/respawn behavior. It also round
trips PNG output through the decoder and validates the built-in font maps.
Tests write to `build/test/<project>/`.

## Gamepad test

```text
dotnet run --project tests/FluxEngine.GamepadTest/FluxEngine.GamepadTest.csproj
```

Connect a controller and follow the on-screen prompts. This project opens a
window and is meant to be run manually, so CI builds it but only executes the
regression suite.

The solution builds both projects; see [../README.md](../README.md) for the
full build and CI workflow.
