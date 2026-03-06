using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using LastfmDiscordRPC2.Utilities;

namespace LastfmDiscordRPC2.Views;

public partial class DialogWindow : Window
{
    private static bool IsCurrentOn { get; set; }

    public DialogWindow()
    {
        InitializeComponent();
        
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Mica, WindowTransparencyLevel.None };

        if (RuntimeLocator.IsWindows11)
        {
            Background = new SolidColorBrush(Colors.Transparent);
        }

        WindowStateProperty.Changed.AddClassHandler<Window>((window, args) =>
        {
            if (window.PlatformImpl == null || (args.NewValue is WindowState state ? state : WindowState.Maximized) != WindowState.Minimized)
            {
                return;
            }
            Close();
            IsCurrentOn = false;
        });

        IsVisibleProperty.Changed.AddClassHandler<Window>((window, args) =>
        {
            if (window.PlatformImpl != null && args.NewValue is true)
            {
                WindowState = WindowState.Normal;
            }
        });

        Closing += (_, _) => IsCurrentOn = false;
    }

    public new static void Show(Window? parent)
    {
        if (IsCurrentOn)
        {
            return;
        }
        IsCurrentOn = true;
        DialogWindow dialogWindow = new DialogWindow();
        Button? btn = dialogWindow.FindControl<Button>("Ok");
        if (btn is not null)
        {
            btn.Click += (_, _) =>
            {
                dialogWindow.Close();
            };
        }

        dialogWindow.Show();
    }
}