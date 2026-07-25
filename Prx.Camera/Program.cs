using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prx.Camera.Records;
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
        builder.Services.Configure<PrxCameraOptions>(builder.Configuration);
        builder.Configuration.AddEnvironmentVariables(prefix: "PRX_");

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddFilter(nameof(TcpLoggerService), LogLevel.Debug);
        
        builder.Services.AddSingleton<ITcpListenerService, TcpListenerService>();
        builder.Services.AddSingleton<IArloProtocolParser, ArloProtocolParser>();
        builder.Services.AddSingleton<IRegistrationHandler, RegistrationHandler>();
        builder.Services.AddSingleton<ITcpLoggerService, TcpLoggerService>();

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