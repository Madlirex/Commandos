namespace Commandos.Commands;

/// <summary>
/// Commands that change the behavior of the console.
/// </summary>
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
    
    /// <summary>
    /// Sets multiple settings of the console to the desired value.
    /// Use: color [target] [color] to change the color of the target (info/user/system/warn/error/background) to a valid ConsoleColor. (red/darkred/blue...)
    /// </summary>
    /// <param name="args">First is the setting to change, next are parameters unique to that setting.</param>
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