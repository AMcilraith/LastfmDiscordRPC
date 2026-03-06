using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Diagnostics;
using System.Runtime;

namespace LastfmDiscordRPC2;

class Program
{
    /// <summary>
    /// When true, the main window is shown on startup. When false (e.g. --no-ui), only the tray icon is used.
    /// </summary>
    public static bool ShowMainWindowOnStartup { get; private set; } = true;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg is "--no-ui" or "-n")
            {
                ShowMainWindowOnStartup = false;
                break;
            }
        }

        var processNames = Process.GetProcessesByName("LastfmDiscordRPC2");
        if (processNames.Length > 1)
        {
            return;
        }
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}