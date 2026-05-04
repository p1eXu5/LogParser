using System;
using LogParser.ElmishApp.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using p1eXu5.Wpf.BootstrapBase;

namespace LogParser.WpfClient;

internal sealed class Bootstrap : BootstrapBase
{
    protected override void ConfigureServices(HostBuilderContext hostBuilderCtx, IServiceCollection services)
    {
        base.ConfigureServices(hostBuilderCtx, services);

        services.TryAddSingleton<ISettingsManager, SettingsManager>();

        AddErrorMessageQueues(services);
    }

    private static void AddErrorMessageQueues(IServiceCollection services)
    {
        Func<IServiceProvider, object?, IErrorMessageQueue> errorMessageQueueFactory = ((sp, key) =>
        {
            ILoggerFactory loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            Microsoft.Extensions.Logging.ILogger logger;

            if (key is string skey)
            {
                logger = loggerFactory.CreateLogger(ErrorMessageQueue.TypeFullName + '.' + skey);
            }
            else
            {
                logger = loggerFactory.CreateLogger<ErrorMessageQueue>();
            }

            return new ErrorMessageQueue(logger);
        });

        services.TryAddKeyedSingleton<IErrorMessageQueue>("main", errorMessageQueueFactory);
        services.TryAddKeyedSingleton<IErrorMessageQueue>("dialog", errorMessageQueueFactory);
    }
}
