using System.Text;

namespace Commandos;

static class Debug
{
    public static ConsoleColor ErrorColor = ConsoleColor.DarkRed;
    public static ConsoleColor WarningColor = ConsoleColor.DarkYellow;
    public static ConsoleColor InfoColor = ConsoleColor.DarkGray;
    public static ConsoleColor LineColor = ConsoleColor.White;
    public static ConsoleColor UserColor = ConsoleColor.Blue;

    public static int MinMessagePrefixPadding = 12;

    public static string IndentString(string msg)
    {
        StringBuilder builder = new StringBuilder();
        if (MinMessagePrefixPadding > msg.Length)
        {
            builder.Append(' ', MinMessagePrefixPadding - msg.Length);
        }

        return msg + builder;
    }
    
    public static void Error(string? msg)
    {
        WritePlainLines($"{msg}".Split("\n"), ErrorColor, "[ ERROR ]");
    }

    public static void Warning(string? msg)
    {
        WritePlainLines($"{msg}".Split("\n"), WarningColor, "[ WARN ]");
    }

    public static void Info(string? msg)
    {
        WritePlainLines($"{msg}".Split("\n"), InfoColor, "[ INFO ]");
    }

    public static void WriteLine(string? msg)
    {
        WritePlainLines($"{msg}".Split("\n"), LineColor, "[ SYSTEM ]");
    }
    
    public static void WritePlainLine(string? msg, ConsoleColor? color = null, string? prefix = null)
    {
        Console.ForegroundColor = color ?? LineColor;
        prefix = prefix == null ? "" : IndentString(prefix);
        Console.WriteLine($"{prefix}{msg}");
    }

    public static void WriteLines(string[] msg, ConsoleColor? color = null)
    {
        if (msg.Length == 0) return;

        Console.ForegroundColor = color ?? LineColor;
        
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

        Console.ForegroundColor = color ?? LineColor;
        prefix = prefix == null ? "" : IndentString(prefix);
        Debug.WritePlainLine(msg[0], color, prefix);
        
        if (msg.Length == 1) return;
        
        foreach (string line in msg[1..])
        {
            Console.WriteLine($"{IndentString("      >>")}{line}");
        }
    }

    public static void Write(string? msg)
    {
        Console.ForegroundColor = LineColor;
        Console.Write(msg);
    }

    public static string? ReadLine(string msg = "[ USER ] ")
    {
        Console.ForegroundColor = UserColor;
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
}