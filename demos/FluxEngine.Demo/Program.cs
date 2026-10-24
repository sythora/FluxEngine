using FluxEngine.Core;
using FluxEngine.Input;

using var engine = new Engine("FluxEngine - WASD Demo", 960, 640);

const int playerSize = 40;
const float playerSpeed = 280;
var playerX = (engine.Window.Width - playerSize) / 2f;
var playerY = (engine.Window.Height - playerSize) / 2f;

engine.Run(
    deltaTime =>
    {
        var horizontal = 0;
        var vertical = 0;

        if (engine.Input.IsKeyDown(Key.A))
            horizontal--;
        if (engine.Input.IsKeyDown(Key.D))
            horizontal++;
        if (engine.Input.IsKeyDown(Key.W))
            vertical--;
        if (engine.Input.IsKeyDown(Key.S))
            vertical++;

        var length = MathF.Sqrt(horizontal * horizontal + vertical * vertical);
        if (length > 0)
        {
            playerX += horizontal / length * playerSpeed * deltaTime;
            playerY += vertical / length * playerSpeed * deltaTime;
        }

        playerX = Math.Clamp(playerX, 0, engine.Window.Width - playerSize);
        playerY = Math.Clamp(playerY, 0, engine.Window.Height - playerSize);

        if (engine.Input.WasKeyPressed(Key.Escape))
            engine.Shutdown();
    },
    () =>
    {
        engine.Window.Clear(18, 22, 32);
        engine.Window.FillRectangle(
            (int)playerX,
            (int)playerY,
            playerSize,
            playerSize,
            90,
            190,
            255);
    });
