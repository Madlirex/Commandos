using System.Text;

namespace Commandos.Commands;

/// <summary>
/// Commands for programming. Cool ik.
/// </summary>
public static class ProgrammingCommands
{
    static Dictionary<string, List<StringBuilder>> _savedSequences = new();

    /// <summary>
    /// Saves the provided sequence as the provided name.
    /// </summary>
    /// <param name="name">Name of sequence.</param>
    /// <param name="args">Sequence to save. First argument (word) is ommited. Such as: save help seq(ommited) help.</param>
    [Command("save", "saveseq")]
    public static void SaveSequence(string name, string[] args)
    {
        List<StringBuilder> sequence = SequenceOfCommands(args[1..]);
        if (!_savedSequences.TryAdd(name, sequence))
        {
            if (Debug.AskSmallRisk(
                    $"A sequence with the name {name} already exists. Do you wish to overwrite it? (Y/N)"))
            {
                _savedSequences[name] = sequence;
            }
        }
    }

    
    /// <summary>
    /// Lists all saves sequences and their names.
    /// </summary>
    [Command("listseq")]
    public static void ListSequences()
    {
        Debug.WriteLine($"List of all saved sequences: ({_savedSequences.Count})");
        foreach (string name in _savedSequences.Keys)
        {
            Debug.WritePlainLine($"{SequenceToString(_savedSequences[name])}", prefix: Debug.IndentString(name + "  ", true));
        }
    }

    
    /// <summary>
    /// Executes saved sequence by given sequence name.
    /// </summary>
    /// <param name="name">Name of saved sequence to execute.</param>
    [Command("execute", "exc", "executeseq")]
    public static void ExecuteSequence(string name)
    {
        if (_savedSequences.TryGetValue(name, out var sequence))
        {
            foreach (StringBuilder sb in sequence)
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
    
    /// <summary>
    /// Creates and runs a new sequence using given commands.
    /// Separate commands with ; such as: seq help; wait 3; q
    /// </summary>
    /// <param name="args">Commands to execute in this sequence.</param>
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

    /// <summary>
    /// Inputs value(s) that are returned by command(s) into the first command.
    /// First argument is command to put values into, next are commands to get values from.
    /// Separate commands with ; just like in sequences.
    /// </summary>
    /// <param name="args">First is command to input values to, second and next are command(s) to get values from.</param>
    [Command("argument", "arg")]
    public static void ArgumentCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Debug.Error($"Not enough commands given, expected at least 2, got: {args.Length}");
            return;
        }
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