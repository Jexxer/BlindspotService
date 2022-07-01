using Microsoft.Win32;
using System.Diagnostics;
using Newtonsoft.Json;

namespace App.WindowsService;

public sealed class WindowsBackgroundService : BackgroundService
{
    private readonly ILogger<WindowsBackgroundService> _logger;
    private readonly Checkin _checkin;
    private readonly Download _download;

    public WindowsBackgroundService(
        Checkin checkin,
        Download download,
        ILogger<WindowsBackgroundService> logger) =>
        (_logger, _checkin, _download) = (logger, checkin, download);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                string servicePath = $"{Directory.GetCurrentDirectory()}\\BlindspotService.exe";
                Config config = new Config();

                // If there is a config.json we need to write the values to Registry then destroy the config.json
                string configPath = $"{Directory.GetCurrentDirectory()}\\config.json";
                _logger.LogInformation($"{configPath}");
                if (OperatingSystem.IsWindows())
                {
                    var blindspotReg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Blindspot", true);
                    if (File.Exists(configPath))
                    {
                        _logger.LogInformation("config.json does exist.", configPath);
                        using(StreamReader r = new StreamReader("config.json"))
                        {
                            string json = r.ReadToEnd();
                            config = JsonConvert.DeserializeObject<Config>(json);
                        }
                        try
                        {
                            _logger.LogInformation("Trying to SetValue of config");
                            blindspotReg.SetValue("test", "test's value");
                            blindspotReg.SetValue("API_KEY", config.api_key);
                            blindspotReg.SetValue("EMAIL", config.email);
                            blindspotReg.SetValue("AGENT_INSTALL_UUID", config.agent_install_uuid);
                            File.Delete(configPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Failed to set Values: ", ex.Message);
                        }
                    } else
                    {
                        _logger.LogInformation("Trying to GetValue of Registry keys");
                        config.api_key = (string)blindspotReg.GetValue("API_KEY");
                        config.email = (string)blindspotReg.GetValue("EMAIL");
                        config.agent_install_uuid = (string)blindspotReg.GetValue("AGENT_INSTALL_UUID");
                    }
                }

                // Check for a pending C2Operation
                bool result = _checkin.GetPendingC2op(config);
                if (result)
                {
                    // Does a agent.exe already exist in this directory?
                    if (File.Exists(servicePath))
                    {
                        if(Process.GetProcessesByName("BlindspotService").Length == 0)
                        {
                            // If BlindspotService.exe exists, start it.
                            Process.Start(servicePath);
                        }
                    }
                    else
                    {
                        // If agent.exe does NOT exist, download, THEN start it.
                        await _download.DownloadAgent(config);
                        Process.Start(servicePath);
                    }


                }
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }
}

public class ConfigJson
{
    public string? api_key { get; set; }
    public string? agent_install_uuid { get; set; }
    public string? email { get; set; }
}