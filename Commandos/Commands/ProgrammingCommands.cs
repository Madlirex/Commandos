using System.Text;

namespace Commandos.Commands;

public static class ProgrammingCommands
{
    public static Dictionary<string, List<StringBuilder>> SavedSequences = new();

    [Command("save", "saveseq")]
    public static void SaveSequence(string name, string[] args)
    {
        List<StringBuilder> sequence = SequenceOfCommands(args[1..]);
        if (SavedSequences.ContainsKey(name))
        {
            if (Debug.AskSmallRisk(
                    $"A sequence with the name {name} already exists. Do you wish to overwrite it? (Y/N)"))
            {
                SavedSequences[name] = sequence;
            }
        }
        else
        {
            SavedSequences.Add(name, sequence);
        }
    }

    [Command("listseq")]
    public static void ListSequences()
    {
        Debug.WriteLine($"List of all saved sequences: ({SavedSequences.Count})");
        foreach (string name in SavedSequences.Keys)
        {
            Debug.WritePlainLine($"{SequenceToString(SavedSequences[name])}", prefix: Debug.IndentString(name + "  ", true));
        }
    }

    [Command("execute", "exc", "executeseq")]
    public static void ExecuteSequence(string name)
    {
        if (SavedSequences.ContainsKey(name))
        {
            foreach (StringBuilder sb in SavedSequences[name])
            {
                CommandRegistry.TryExecuteCommand(sb.ToString(), out object? var);
                Debug.Info(var?.ToString());
            }
        }
        else
        {
            Debug.Error($"No sequence with name {name}.");
        }
    }
    
    internal static string SequenceToString(List<StringBuilder> sequence)
    {
        return string.Join("; ", sequence.Select(x => x.ToString()).ToArray());
    }
    
    [Command("sequence", "seq")]
    public static List<StringBuilder> SequenceOfCommands(string[] args)
    {
        List<StringBuilder> commands = [new()];
        foreach (string s in args)
        {
            if (s.Contains(';'))
            {
                string[] split = s.Split(';');
                commands[^1].Append(split[0]);
                foreach (string sub in split[1..])
                {
                    commands.Add(new StringBuilder(sub));
                }
            }
            else
            {
                commands[^1].Append(s + " ");
            }
        }
        if (commands[^1].Length > 0) commands[^1].Remove(commands[^1].Length - 1, 1);

        return commands;
    }

    [Command("argument", "arg")]
    public static void ArgumentCommand(string[] args)
    {
        List<StringBuilder> commands = SequenceOfCommands(args);
        object[] arguments = new object[commands.Count - 1];

        for (int i = 1; i < commands.Count; i++)
        {
            CommandRegistry.TryExecuteCommand(commands[i].ToString(), out arguments[i - 1]);
        }

        foreach (object obj in arguments)
        {
            commands[0].Append(" " + obj);
        }
        CommandRegistry.TryExecuteCommand(commands[0].ToString(), out _);
    }
}