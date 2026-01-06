using System.Text;
using Commandos.Commands;

namespace Commandos;

static class Debug
{
    public static int MinMessagePrefixPadding = 12;

    public static string IndentString(string msg, bool fromLeft = false)
    {
        StringBuilder builder = new StringBuilder();
        if (MinMessagePrefixPadding > msg.Length)
        {
            builder.Append(' ', MinMessagePrefixPadding - msg.Length);
        }

        return fromLeft ? builder + msg : msg + builder;
    }
    
    public static void Error(string? msg)
    {
        WritePlainLines($"{msg}".Split("\n"), Settings.ConsoleColors["error"], "[ ERROR ]");
    }

    public static void Warning(string? msg)
    {
        WritePlainLines($"{msg}".Split("\n"), Settings.ConsoleColors["warn"], "[ WARN ]");
    }

    public static void Info(string? msg)
    {
        WritePlainLines($"{msg}".Split("\n"), Settings.ConsoleColors["info"], "[ INFO ]");
    }

    public static void WriteLine(string? msg)
    {
        WritePlainLines($"{msg}".Split("\n"), Settings.ConsoleColors["system"], "[ SYSTEM ]");
    }
    
    public static void WritePlainLine(string? msg, ConsoleColor? color = null, string? prefix = null)
    {
        Console.ForegroundColor = color ?? Settings.ConsoleColors["system"];
        prefix = prefix == null ? "" : IndentString(prefix);
        Console.WriteLine($"{prefix}{msg}");
    }

    public static void WriteLines(string[] msg, ConsoleColor? color = null)
    {
        if (msg.Length == 0) return;

        Console.ForegroundColor = color ?? Settings.ConsoleColors["system"];
        
        Debug.WriteLine(msg[0]);
        
        if (msg.Length == 1) return;
        
        foreach (string line in msg[1..])
        {
            Console.WriteLine($"{IndentString("      >>")}{line}");
        }
    }
    
    public static void WritePlainLines(string[] msg, ConsoleColor? color = null, string? prefix = null)
    {
        if (msg.Length == 0) return;

        Console.ForegroundColor = color ?? Settings.ConsoleColors["system"];
        prefix = prefix == null ? "" : IndentString(prefix);
        WritePlainLine(msg[0], color, prefix);
        
        if (msg.Length == 1) return;
        
        foreach (string line in msg[1..])
        {
            Console.WriteLine($"{IndentString("      >>")}{line}");
        }
    }

    public static void Write(string? msg)
    {
        Console.ForegroundColor = Settings.ConsoleColors["system"];
        Console.Write(msg);
    }

    public static string? ReadLine(string msg = "[ USER ] ")
    {
        Console.ForegroundColor = Settings.ConsoleColors["user"];
        Console.Write(IndentString(msg));
        return Console.ReadLine();
    }

    public static bool AskSmallRisk(string msg = "This action may take a while. Are you sure you want to proceed? (Y/N)")
    {
        Warning(msg);
        return ReadLine()!.ToLower() == "y";
    }

    public static bool AskDanger(string msg = "This action may be dangerous. Are you sure you want to proceed? (Y/N)")
    {
        WritePlainLine(msg, ConsoleColor.DarkRed, "[ CAUTION ]");
        return ReadLine()!.ToLower() == "y";
    }

    public static void WaitSeconds(int seconds)
    {
        WaitSeconds((float)seconds);
    }

    public static void WaitSeconds(float seconds)
    {
        Thread.Sleep((int)(seconds * 1000));
    }

    public static void Wait(int milliseconds)
    {
        WaitSeconds(milliseconds * 1000);
    }
}