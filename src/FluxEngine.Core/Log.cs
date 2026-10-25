namespace FluxEngine.Core;

// dumb console logger, better than Console.WriteLine all over the place
public static class Log
{
    public static void Info(string message) => Write("info", ConsoleColor.Gray, message);
    public static void Warn(string message) => Write("warn", ConsoleColor.Yellow, message);
    public static void Error(string message) => Write("error", ConsoleColor.Red, message);

    private static void Write(string level, ConsoleColor color, string message)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
        Console.ForegroundColor = previous;
    }
}
