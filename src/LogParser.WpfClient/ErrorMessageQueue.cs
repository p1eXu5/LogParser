using System;
using LogParser.ElmishApp.Interfaces;
using Microsoft.Extensions.Logging;

namespace LogParser.WpfClient;

public sealed class ErrorMessageQueue : MaterialDesignThemes.Wpf.SnackbarMessageQueue, IErrorMessageQueue
{
    private readonly ILogger _logger;

    internal static string TypeFullName { get; } = typeof(ErrorMessageQueue).FullName!;

    public ErrorMessageQueue(ILogger logger)
    {
        _logger = logger;
    }

    public void EnqueueError(string value)
    {
        _logger.LogError(value);

        Enqueue(
            value,
            "Clear",
            _ => Clear(),
            null,
            false,
            true,
            TimeSpan.FromSeconds(15.0)
        );
    }
}
