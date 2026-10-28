# Regression tests

Run the engine and scene-editor regression suite from the repository root:

```text
dotnet run --project tests/FluxEngine.RegressionTests/FluxEngine.RegressionTests.csproj
```

It checks editor scene save/reopen/save consistency, engine/editor scene format compatibility, box and circle collision queries, deferred component/entity lifecycle, timers, level text loading/saving and malformed data, and game-manager score/death/respawn behavior. It also rounds trips PNG output through the decoder and validates the built-in font maps. This console test project targets `net10.0-windows` because the current editor project uses Windows Forms.
