import argparse
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "src" / "FluxEngine.csproj"


def check(condition, message):
    if not condition:
        raise AssertionError(message)


def run_build(configuration):
    result = subprocess.run(
        [
            "dotnet",
            "build",
            str(PROJECT),
            "--configuration",
            configuration,
            "--nologo",
        ],
        cwd=ROOT,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
    )
    if result.returncode:
        print(result.stdout)
        raise RuntimeError(f"FluxEngine build failed with exit code {result.returncode}")


def test_input_manager(dll):
    try:
        from pythonnet import load
    except ImportError as error:
        raise RuntimeError(
            "pythonnet is required to call the managed FluxEngine DLL. "
            "Install it with: python -m pip install pythonnet"
        ) from error

    try:
        load("coreclr")
        import clr
    except Exception as error:
        raise RuntimeError(
            "Could not start .NET CoreCLR from Python. Check that the .NET 10 runtime "
            "is installed and pythonnet is installed in this Python environment."
        ) from error

    clr.AddReference(str(dll))
    from FluxEngine.Input import InputManager, Key, MouseButton

    input_state = InputManager()
    check(not input_state.IsKeyDown(Key.A), "A should start up")
    check(not input_state.WasKeyPressed(Key.A), "A should not start pressed")

    input_state.SetKey(Key.A, True)
    input_state.SetKey(Key.A, True)
    check(input_state.IsKeyDown(Key.A), "A should be held")
    check(input_state.WasKeyPressed(Key.A), "A should only report a new press once")
    check(input_state.AnyKeyDown and input_state.AnyKeyPressed, "key summary state should be true")

    input_state.BeginFrame()
    check(input_state.IsKeyDown(Key.A), "A should stay held after BeginFrame")
    check(not input_state.WasKeyPressed(Key.A), "held A should not be newly pressed next frame")
    input_state.SetKey(Key.A, False)
    check(not input_state.IsKeyDown(Key.A), "A should be up after release")
    check(input_state.WasKeyReleased(Key.A), "A release should be reported this frame")
    input_state.SetKey(Key.A, False)
    check(input_state.WasKeyReleased(Key.A), "duplicate key-up should not erase release state")

    input_state.BeginFrame()
    check(not input_state.WasKeyReleased(Key.A), "release state should clear next frame")
    input_state.SetKey(Key.Unknown, True)
    check(not input_state.AnyKeyDown, "unknown key should be ignored")

    input_state.SetMouseButton(MouseButton.Left, True)
    input_state.SetMouseButton(MouseButton.Left, True)
    check(input_state.IsMouseButtonDown(MouseButton.Left), "left mouse button should be held")
    check(input_state.WasMouseButtonPressed(MouseButton.Left), "left mouse button should press once")
    input_state.BeginFrame()
    check(input_state.IsMouseButtonDown(MouseButton.Left), "left button should stay held")
    check(
        not input_state.WasMouseButtonPressed(MouseButton.Left),
        "held mouse button should not press again",
    )
    input_state.SetMouseButton(MouseButton.Left, False)
    check(
        input_state.WasMouseButtonReleased(MouseButton.Left),
        "left mouse release should be reported",
    )

    input_state.SetMousePosition(10, 20)
    check(input_state.MouseX == 10 and input_state.MouseY == 20, "mouse position should be stored")
    check(
        input_state.MouseDeltaX == 0 and input_state.MouseDeltaY == 0,
        "first mouse position should have no delta",
    )
    input_state.SetMousePosition(15, 27)
    input_state.SetMousePosition(19, 30)
    check(input_state.MouseDeltaX == 9 and input_state.MouseDeltaY == 10, "mouse deltas should add up")

    input_state.AddMouseScroll(1.5, -2.0)
    input_state.AddMouseScroll(0.5, 3.0)
    check(input_state.ScrollX == 2.0 and input_state.ScrollY == 1.0, "scroll should add up")
    input_state.BeginFrame()
    check(
        input_state.MouseDeltaX == 0 and input_state.MouseDeltaY == 0,
        "mouse delta should clear each frame",
    )
    check(input_state.ScrollX == 0 and input_state.ScrollY == 0, "scroll should clear each frame")
    check(input_state.MouseX == 19 and input_state.MouseY == 30, "mouse position should persist")

    input_state.SetKey(Key.W, True)
    input_state.SetMouseButton(MouseButton.Right, True)
    input_state.SetFocus(False)
    check(not input_state.HasFocus, "focus loss should update focus state")
    check(not input_state.IsKeyDown(Key.W), "focus loss should clear held keys")
    check(
        not input_state.IsMouseButtonDown(MouseButton.Right),
        "focus loss should clear held mouse buttons",
    )
    input_state.SetFocus(True)
    check(input_state.HasFocus, "focus gain should update focus state")


def main():
    parser = argparse.ArgumentParser(
        description="Call InputManager in the built FluxEngine DLL from Python."
    )
    parser.add_argument(
        "--configuration",
        default="Debug",
        help="Build configuration to test (default: Debug)",
    )
    args = parser.parse_args()

    run_build(args.configuration)
    dll = (
        ROOT
        / "artifacts"
        / "bin"
        / args.configuration
        / "net10.0"
        / "FluxEngine.dll"
    )
    if not dll.is_file():
        raise FileNotFoundError(f"Built FluxEngine DLL not found: {dll}")

    test_input_manager(dll)
    print("InputManager DLL tests passed")


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        print(f"input test failed: {error}", file=sys.stderr)
        sys.exit(1)
