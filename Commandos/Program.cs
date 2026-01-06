using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Commandos;

static class Program
{
    public static string? ApplicationVersion;
    public static string? DevStudio;
    public static bool Running = true;

    #region OnlyGodKnows
    [DllImport("kernel32.dll")]
    static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
    #endregion
    
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
            CommandRegistry.TryExecuteCommand(input, out object? var);
            if (var != null) Debug.Write(var.ToString());
        }
    }

    public static void GetCurrentVersionInfo()
    {
        ApplicationVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
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
