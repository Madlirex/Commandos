using System.Reflection;

namespace Commandos;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
class CommandAttribute(string name, params string[] aliases) : Attribute
{
    public string Name { get; } = name;
    public string[]  Aliases { get; } = aliases;
}

static class CommandRegistry
{
    public static Dictionary<string, MethodInfo> Commands = new Dictionary<string, MethodInfo>();

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
}