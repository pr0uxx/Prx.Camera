using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prx.Camera.Services;

namespace Prx.Camera;

public static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (VersionOnly(args, out var version))
        {
            Console.WriteLine(version);
            return 0;
        }
        
        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        
        builder.Services.AddSingleton<ITcpListenerService, TcpListenerService>();
        builder.Services.AddSingleton<IArloProtocolParser, ArloProtocolParser>();
        builder.Services.AddSingleton<IRegistrationHandler, RegistrationHandler>();

        builder.Services.AddHostedService<BaseStationService>();

        var app = builder.Build();

        await app.RunAsync();

        return 0;
    }

    private static bool VersionOnly(string[] args, out string? version)
    {
        version = null;
        
        if (args.Length > 0 && args[0] == "--version")
        {
            version = AppVersion.Version;
            return true;
        }

        return false;
    }
}