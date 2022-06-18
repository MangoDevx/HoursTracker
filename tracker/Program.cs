﻿using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using tracker.Database;
using tracker.Services;

Process? proc = null;
#if DEBUG
Console.WriteLine("Remember to manually start the API");
#else
if (!File.Exists("../api/TrackerApi.exe") && !File.Exists("../api/TrackerApi.dll"))
{
    Console.WriteLine("Api not found. Webpanel will not work.");
}
else
{
    var isExe = File.Exists("../api/TrackerApi.exe");
    if (isExe)
    {
        var processInfo = new ProcessStartInfo("../api/TrackerApi.exe")
        {
            WorkingDirectory = "../api",
            CreateNoWindow = true,
            UseShellExecute = false
        };
        proc = Process.Start(processInfo);
        Console.WriteLine("Api hidden. To manually kill it go to task manager and kill TrackerApi.exe. Exit App command should also take care of it.");
    }
    else
    {
        var processInfo = new ProcessStartInfo("/bin/bash", "dotnet ../api/TrackerApi.dll")
        {
            WorkingDirectory = "../api",
            UseShellExecute = false
        };
        proc = Process.Start(processInfo);
        Console.WriteLine("Api launched. You may need to manually kill it, or use the Exit Command app from the main menu.");
    }
}
#endif

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((_, logging) =>
    {
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddConsole();
        logging.AddFilter("Microsoft.EntityFrameworkCore.*", LogLevel.Warning);
        logging.AddFilter("Microsoft.Hosting.*", LogLevel.Warning);
    })
    .ConfigureServices((context, services) =>
    {
        var cString = context.Configuration.GetConnectionString("Sqlite");
        services
            .AddSqlite<DataContext>(cString)
            .AddSingleton(new HttpClient())
            .AddScoped<DbInitService>()
            .AddScoped<GameDetectionService>()
            .AddHostedService<TrackingService>()
            .AddHostedService<ConsoleService>();
    })
    .Build();

await host.Services.GetRequiredService<DbInitService>().InitializeDatabaseAsync();
await host.Services.GetRequiredService<GameDetectionService>().StartAutomaticDetectionAsync();
await host.RunAsync();

if(proc is { HasExited: false })
    proc.Kill();