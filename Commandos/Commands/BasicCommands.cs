using System.Globalization;
using System.Reflection;

namespace Commandos.Commands;

public static class BasicCommands
{
    private static bool _askDangerCommands = true;
    private static int _dangerRecursionLimit = 4;
    private static int _dangerRepetitionLimit = 1000;
    
    /// <summary>
    /// Without a parameter lists all commands.
    /// With a parameter lists description of the command provided from the XML description of the method.
    /// </summary>
    /// <param name="command">Command to get information about.</param>
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
            string[] descriptionText = XmlUtils.GetSummary(CommandRegistry.Commands[command]).Split("\n");
            
            CommandAttribute attribute = CommandRegistry.Commands[command].GetCustomAttribute<CommandAttribute>()!;
            string[] prefix = [$"Description for command {attribute.Name}:"];
            string usage = $"Usage: {XmlUtils.ParametersToString(XmlUtils.GetParameters(CommandRegistry.GetCommand(command)))}";
            string suffix = $"Aliases ({attribute.Aliases.Length}): {string.Join(", ", attribute.Aliases)}";
            if (attribute.Aliases.Length == 0) suffix = $"Aliases (0): None";
            
            Debug.WriteLines(prefix.Concat(descriptionText).ToArray());
            Debug.WritePlainLines(usage.Split("\n"), prefix: "      >>");
            Debug.WritePlainLine(suffix, prefix: "      >>");
        }
    }
    
    /// <summary>
    /// Quits
    /// </summary>
    [Command("q", "exit", "quit")]
    public static void Quit()
    {
        Debug.Info("Fuck you.");
        Debug.WaitSeconds(0.1f);
        Program.Running = false;
    }

    
    /// <summary>
    /// Repeats given string arguments.
    /// Each argument will be in a new line. To write a string with spaces use "".
    /// If the first argument is a valid integer number, the arguments after will be repeated that many times.
    /// </summary>
    [Command("echo")]
    public static string[] Echo(string[] args)
    {
        if (args.Length != 0)
        {
            if (int.TryParse(args[0], out int count))
            {
                if (_askDangerCommands && count >= _dangerRepetitionLimit)
                {
                    if (!Debug.AskSmallRisk()) return [];
                }
                for (; count > 0; count--)
                {
                    return args[1..];
                }

                return args;
            }
        }
        Debug.WriteLines(args);
        return args;
    }

    
    /// <summary>
    /// Adds together all numbers provided.
    /// They can be integer or float, positive or negative. Use commas for float numbers.
    /// </summary>
    [Command("add", "sum")]
    public static float Add(float[] nums)
    {
        Debug.WriteLine((nums.Sum()).ToString(CultureInfo.CurrentCulture));
        return nums.Sum();
    }
    
    /// <summary>
    /// Lists all subdirectories of provided directory.
    /// </summary>
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
    
    /// <summary>
    /// Clears the console.
    /// </summary>
    [Command("clear")]
    public static void Clear()
    {
        Console.Clear();
    }
    
    /// <summary>
    /// Waits for x seconds.
    /// </summary>
    [Command("wait", "sleep")]
    public static void Wait(float seconds)
    {
        Debug.WaitSeconds(seconds);
    }
}