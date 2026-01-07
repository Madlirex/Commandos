using System.Reflection;
using System.Text;

namespace Commandos;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
class CommandAttribute(string name, params string[] aliases) : Attribute
{
    public string Name { get; } = name;
    public string[]  Aliases { get; } = aliases;
}

static class CommandRegistry
{
    public static Dictionary<string, MethodInfo> Commands = new();

    public static void RegisterCommands()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
        {
            foreach (MethodInfo method in type.GetMethods())
            {
                CommandAttribute? attribute = method.GetCustomAttribute<CommandAttribute>();
                if (attribute == null) continue;
                
                Commands[attribute.Name] = method;
                foreach (string alias in attribute.Aliases)
                {
                    Commands[alias] = method;
                }
            }
        }
    }

    public static MethodInfo GetCommand(string commandName)
    {
        return Commands[commandName];
    }
    
    public static string GetCommandName(MethodInfo method)
    {
        CommandAttribute? attribute = method.GetCustomAttribute<CommandAttribute>();
        if (attribute == null) throw new Exception($"Method {method.Name} doesn't have Command atrribute.");
        return attribute.Name;
    }

    public static string GetCommandName(string alias)
    {
        return GetCommandName(GetCommand(alias));
    }

    public static bool TryExecuteCommand(string command, out object? output)
    {
        output = null;
        try
        {
            if (!ParseArguments(command, out output))
            {
                Debug.Error("Unknown command: " + command +
                            "\nTry using \"help\" to see more commands or \"q\" to exit.");
            }
        }
        catch (KeyNotFoundException)
        {
            Debug.Error("Unknown command: " + command +
                        "\nTry using \"help\" to see more commands or \"q\" to exit.");
            return false;
        }
        catch (Exception e)
        {
            Debug.Error(e.ToString());
            return false;
        }

        return true;
    }
    
    public static bool ParseArguments(string input, out object? output)
    {
        output = null;
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

        output = commandInfo.Invoke(null, convertedArgs.ToArray());
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
}