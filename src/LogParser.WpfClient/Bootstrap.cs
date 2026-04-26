using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using p1eXu5.Wpf.BootstrapBase;

namespace LogParser.WpfClient;

internal sealed class Bootstrap : BootstrapBase
{
    protected override void ConfigureServices(HostBuilderContext hostBuilderCtx, IServiceCollection services)
    {
        base.ConfigureServices(hostBuilderCtx, services);
    }
}
