using LogParser.ElmishApp.Interfaces;

namespace LogParser.WpfClient;

public sealed class ErrorMessageQueue : MaterialDesignThemes.Wpf.SnackbarMessageQueue, IErrorMessageQueue
{
    public void EnqueueError(string value)
    {
        Enqueue(value);
    }
}
