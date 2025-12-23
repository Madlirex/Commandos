using System.Reflection;

namespace Commandos;

public static class BasicCommands
{
    private static bool _askDangerCommands = true;
    private static int _dangerRecursionLimit = 4;
    private static int _dangerRepetitionLimit = 1000;
    
    /// <summary>
    /// Without a parameter lists all commands.
    /// With a parameter lists description of the command provided from the XML description of the method.
    /// </summary>
    /// <param name="command">Command to get description of. Leave empty for list of commands.</param>
    [Command("help", "commands")]
    public static void Help(string command = "")
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            string[] prefixText =
                ["Here is a full list of commands. To get information about a command run \"help <command>\"."];
            Debug.WriteLines(prefixText.Concat(CommandRegistry.Commands.Keys.ToArray()).ToArray());
        }
        else
        {
            string[] prefix = [$"Description for command {command}:"];
            
            string[] descriptionText = XmlUtils.GetSummary(CommandRegistry.Commands[command]).Split("\n");
            
            CommandAttribute attribute = CommandRegistry.Commands[command].GetCustomAttribute<CommandAttribute>()!;
            string[] suffix = [$"Aliases ({attribute.Aliases.Length}): {string.Join(", ", attribute.Aliases)}"];
            if (attribute.Aliases.Length == 0) suffix = [$"Aliases (0): None"];
            
            Debug.WriteLines(prefix.Concat(descriptionText).ToArray().Concat(suffix).ToArray());
        }
    }
    /// <summary>
    /// Quits
    /// </summary>
    [Command("q", "exit", "quit")]
    public static void Quit()
    {
        Program.Running = false;
    }

    [Command("echo")]
    public static void Echo(string[] args)
    {
        if (args.Length != 0)
        {
            if (int.TryParse(args[0], out var count))
            {
                if (_askDangerCommands && count >= _dangerRepetitionLimit)
                {
                    if (!Debug.AskSmallRisk()) return;
                }
                for (; count > 0; count--)
                {
                    Debug.WriteLines(args[1..]);
                }

                return;
            }
        }
        Debug.WriteLines(args);
    }

    [Command("add", "sum")]
    public static void Add(int[] nums)
    {
        Debug.WriteLine((nums.Sum()).ToString());
    }

    [Command("listdir")]
    public static int[]? ListDir(string directory, bool deep = false, int maxDepth = -1, int depth = 0)
    {
        if (depth == 0 && (maxDepth == -1 || maxDepth >= _dangerRecursionLimit) && _askDangerCommands && deep)
        {
            if (!Debug.AskDanger()) return null;
        }

        int[] counter = new int[2];

        try
        {
            string[] dirs = Directory.GetDirectories(directory);
            Debug.WriteLines(dirs);
            counter[0] += dirs.Length;

            if (!deep || (maxDepth == depth && maxDepth != -1))
            {
                return counter;
            }

            foreach (string dir in dirs)
            {
                int[] counts = ListDir(dir, deep, maxDepth, depth + 1)!;
                counter[0] += counts[0];
                counter[1] += counts[1];
            }
        }
        catch (UnauthorizedAccessException)
        {
            Debug.Warning($"Access denied for {directory}.");
            counter[1] += 1;
        }
        catch (DirectoryNotFoundException)
        {
            Debug.Error($"No such directory: {directory}.");
        }
        
        if (depth == 0)
        {
            Debug.Info($"Successfully listed {counter[0]} directories with {counter[1]} denied accesses.");
        }

        return counter;
    }

    [Command("settings", "options")]
    public static void Settings(string[] args)
    {
        bool successful = true;
        switch (args[0])
        {
            case "color":
                ConsoleColor color;
                if (Enum.TryParse(args[2], true, out color))
                {
                    switch (args[1])
                    {
                        case "user":
                            Debug.UserColor = color;
                            break;
                        case "warn":
                            Debug.WarningColor = color;
                            break;
                        case "error":
                            Debug.ErrorColor = color;
                            break;
                        case "info":
                            Debug.InfoColor = color;
                            break;
                        case "system":
                            Debug.LineColor = color;
                            break;
                        default:
                            Debug.Error($"No such color settings target: {args[1]}. Try using user, warn, error, info or system.");
                            successful = false;
                            break;
                    }
                    if (successful) Debug.Info($"Successfully set the color of {args[1]} to {args[2]}.");
                }
                else
                {
                    Debug.Error($"No such color: {args[2]}.");
                }

                break;
        }
    }
    
    [Command("clear")]
    public static void Clear()
    {
        Console.Clear();
    }
}