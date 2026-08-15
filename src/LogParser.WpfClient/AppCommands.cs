using System.Windows.Input;

namespace LogParser.WpfClient;

internal static class AppCommands
{
    private static RoutedUICommand triggerRawLogsInputVisibility;

    static AppCommands()
    {
        // Initialize the command.
        InputGestureCollection inputs = new InputGestureCollection();
        inputs.Add(new KeyGesture(Key.R, ModifierKeys.Control, "Ctrl+R"));

        triggerRawLogsInputVisibility = new RoutedUICommand(
        "Show/Hide Raw Logs", nameof(TriggerRawLogsInputVisibility), typeof(AppCommands), inputs);
    }
    public static RoutedUICommand TriggerRawLogsInputVisibility
        => triggerRawLogsInputVisibility;
}
