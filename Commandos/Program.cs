using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;

namespace Commandos;

static class Program
{
    public static string? ApplicationVersion;
    public static string? DevStudio;
    public static bool Running = true;
    
    [DllImport("kernel32.dll")]
    static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
    
    static void Main(string[] args)
    {
        EnableAnsi();
        GetCurrentVersionInfo();
        CommandRegistry.RegisterCommands();
        
        Debug.Info($"Commandos v. {ApplicationVersion}. Created {DevStudio} {DateTime.Now.Year}. All right reserved.");
        Debug.WriteLine("Welcome to the little console thingy.");
        Debug.WriteLine("For more information type \"help\" or \"commands\".");
        Debug.WriteLine("To exit write \"q\".");
        while (Running)
        {
            string? input = Debug.ReadLine();
            if (input == null)  continue;

            try
            {
                if (!ParseArguments(input))
                {
                    Debug.Error("Unknown command: " + input +
                                "\nTry using \"help\" to see more commands or \"q\" to exit.");
                }
            }
            catch (KeyNotFoundException)
            {
                Debug.Error("Unknown command: " + input +
                            "\nTry using \"help\" to see more commands or \"q\" to exit.");
            }
            catch (Exception e)
            {
                Debug.Error(e.ToString());
            }
        }
    }

    public static bool ParseArguments(string input)
    {
        bool omit = false;
        bool isString = false;
        StringBuilder last = new StringBuilder();
        List<string> arguments = [];
        
        foreach (char c in input)
        {
            switch (c)
            {
                case ' ' when !(omit || isString):
                    arguments.Add(last.ToString());
                    last = new StringBuilder();
                    break;
                case '"' when !omit:
                    isString = !isString;
                    break;
                case '\\' when !omit:
                    omit = true;
                    break;
                default:
                    if (omit && c != '\\' && c != '"' && c != ' ') last.Append('\\');
                    omit = false;
                    last.Append(c);
                    break;
            }
        }
        arguments.Add(last.ToString());

        if (arguments.Count == 0) return true;

        string commandName = arguments[0];
        MethodInfo commandInfo = CommandRegistry.Commands[commandName];
        arguments.RemoveAt(0);
        
        ParameterInfo[] parameters = commandInfo.GetParameters();
        List<object> convertedArgs = [];
        
        for (int i = 0; i < parameters.Length; i++)
        {
            Type targetType = parameters[i].ParameterType;

            if (targetType.IsArray)
            {
                if (i != parameters.Length - 1)
                {
                    Debug.Error($"Command {commandName} parameter error: Array should be last. \nThis is not user error, but developer issue.");
                    return false;
                }
                
                Type elementType = targetType.GetElementType()!;
                int remaining = arguments.Count - i;
                Array array = Array.CreateInstance(elementType, remaining);

                for (int j = 0; j < remaining; j++)
                {
                    try
                    {
                        array.SetValue(ConvertArgument(arguments[i+j], elementType), j);   
                    }
                    catch (Exception e)
                    {
                        Debug.Error($"Invalid value for '{parameters[i].Name}': {e.Message}");
                        return false;
                    }
                }

                convertedArgs.Add(array);
                break;
            }

            if (i >= arguments.Count)
            {
                if (parameters[i].HasDefaultValue)
                {
                    convertedArgs.Add(parameters[i].DefaultValue!);
                    continue;
                }
               
                Debug.Error($"Missing required parameter {parameters[i].Name}.");
                return false;
                
            }
            
            string rawValue = arguments[i];
            try
            {
                convertedArgs.Add(ConvertArgument(rawValue, targetType));
            }
            catch (Exception ex)
            {
                Debug.Error(
                    $"Invalid value for '{parameters[i].Name}': {ex.Message}");
                return false;
            }
        }

        commandInfo.Invoke(null, convertedArgs.ToArray());
        return true;
        
    }

    public static object ConvertArgument(string value, Type targetType)
    {
        if (targetType == typeof(string))
            return value;

        if (targetType == typeof(int))
            return int.Parse(value);

        if (targetType == typeof(float))
            return float.Parse(value);

        if (targetType == typeof(double))
            return double.Parse(value);

        if (targetType == typeof(bool))
            return bool.Parse(value);
        
        if (targetType.IsEnum)
            return Enum.Parse(targetType, value, ignoreCase: true);

        throw new NotSupportedException(
            $"Type '{targetType.Name}' is not supported.");
    }

    public static void GetCurrentVersionInfo()
    {
        ApplicationVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
        DevStudio = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).CompanyName;
    }
    
    static void EnableAnsi()
    {
        const int STD_OUTPUT_HANDLE = -11;
        const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        var handle = GetStdHandle(STD_OUTPUT_HANDLE);
        GetConsoleMode(handle, out uint mode);
        SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
    }
}
