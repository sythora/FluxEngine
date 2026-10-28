# Interactive gamepad test

Run the test window from the repository root:

```sh
dotnet run --project tests/FluxEngine.GamepadTest/FluxEngine.GamepadTest.csproj
```

A readable native Windows test window auto-detects connected controllers and presents the matching hardware diagram:
- **PlayStation**: Displays DualShock / DualSense layout with symmetrical parallel thumbsticks, D-Pad, Cross/Circle/Square/Triangle face buttons, L1/L2/R1/R2 triggers, Share/Create, Options, PS guide button, and live interactive Touchpad with click tracking.
- **Xbox**: Displays Xbox Series / One / 360 layout with authentic asymmetrical thumbsticks (left stick top-left, D-Pad bottom-left, right stick bottom-right), A/B/X/Y colored buttons, LB/RB bumpers, LT/RT analog triggers, View, Menu, Share, and Xbox Guide button.
- **Nintendo**: Displays Switch Pro layout with Nintendo button orientations (B/A/Y/X), L/ZL/R/ZR, Minus/Plus, Home, and Capture.

### Controls & Features:
- **Auto-Detection**: Automatically identifies connected hardware via SDL2 controller type, USB/Bluetooth vendor and product IDs, and mapping metadata.
- **Layout Tabs**: Clickable UI tabs at the top-right (`Auto`, `PlayStation`, `Xbox`, `Nintendo`) or press `Tab` / `1`-`4` to preview and test any layout.
- **Live Inputs**: Buttons turn amber while held and green once tested. Sticks move in real-time with full 360° analog deflection. Triggers fill smoothly with live pressure gauges.
- **Rumble Testing**: Press `Space` or click **Test Rumble** to test maximum strength controller vibration (100% power across heavy and light motors).
- **Reset**: Press `R`, `Backspace`, `Delete`, or click **Reset** to clear tested button highlights.
