using LogParser.ElmishApp.Interfaces;

namespace LogParser.WpfClient;

public sealed class ErrorMessageQueue : MaterialDesignThemes.Wpf.SnackbarMessageQueue, IErrorMessageQueue
{
    public void EnqueuError(string value)
    {
        Enqueue(value);
    }
}
