namespace Commandos.Commands;

public static class Settings
{
    /// <summary>
    /// Colors for the console.
    /// </summary>
    public static Dictionary<string, ConsoleColor> ConsoleColors = new Dictionary<string, ConsoleColor>
    {
        {"error", ConsoleColor.DarkRed},
        {"warn", ConsoleColor.DarkYellow},
        {"info", ConsoleColor.DarkGray},
        {"system", ConsoleColor.White},
        {"user", ConsoleColor.Blue},
        {"background", ConsoleColor.Black}
    };
    
    [Command("settings", "options")]
    public static void SettingsCommand(string[] args)
    {
        switch (args[0])
        {
            case "color":
                ConsoleColor color;
                if (Enum.TryParse(args[2], true, out color))
                {
                    if (ConsoleColors.ContainsKey(args[1]))
                    {
                        ConsoleColors[args[1]] = color;
                        if (args[1] == "background") Console.BackgroundColor = ConsoleColors[args[1]];
                        Debug.Info($"Successfully set the color of {args[1]} to {args[2]}.");
                    }
                    else
                    {
                        Debug.Error($"No such color settings target: {args[1]}. Try using user, warn, error, info or system.");
                    }
                }
                else
                {
                    Debug.Error($"No such color: {args[2]}.");
                }
                break;
        }
    }
}