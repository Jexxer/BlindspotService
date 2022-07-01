using App.WindowsService;

using IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Blindspot Service";
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<Checkin>();
        services.AddSingleton<Download>();
        services.AddHostedService<WindowsBackgroundService>();
    })
    .Build();

await host.RunAsync();