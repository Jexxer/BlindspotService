using Blindspot.Services;
using Microsoft.Win32;
using System.Diagnostics;

namespace App.WindowsService;

public sealed class WindowsBackgroundService : BackgroundService
{
    private readonly ILogger<WindowsBackgroundService> _logger;
    private readonly Checkin _checkin;
    private readonly Download _download;
    private readonly TimeSpan checkinDelay = TimeSpan.FromMinutes(10);
    public Campaign campaign = new Campaign();

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
                //_logger.LogError($"campaign_uuid: {campaign.CampaignUUID}");
                Config config = new Config();
                if (OperatingSystem.IsWindows())
                {

                    var blindspotReg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Blindspot");
                    if (blindspotReg != null)
                    {
                        _logger.LogDebug($"in if statement blingsptReg not null");
                        // decrypt api_key
                        var encryptionService = new EncryptionService();
                        var encryptedApiKey = (string?)blindspotReg.GetValue("API_KEY") ?? "";

                        string apiKey = encryptionService.Decrypt(encryptedApiKey);
                        config.api_key = apiKey;
                        _logger.LogDebug($"api_key: {apiKey}");
                        config.email = (string?)blindspotReg.GetValue("EMAIL");
                        config.agent_install_uuid = (string?)blindspotReg.GetValue("AGENT_INSTALL_UUID");
                        config.path = (string?)blindspotReg.GetValue("DIR");
                    }
                }
                string agentPath = $"{config.path}\\blindspotagent.exe";
                // Check for a pending C2Operation
                bool result = _checkin.GetPendingC2op(config, campaign);
                if (result)
                {
                    // Does a agent.exe already exist in this directory?
                    if (File.Exists(agentPath))
                    {
                        // if agent is downloaded but not running
                        if(Process.GetProcessesByName("blindspotagent").Length == 0)
                        {
                            // start it.
                            Process agent = new Process();
                            agent.StartInfo.FileName = agentPath;
                            agent.StartInfo.Arguments = "-service";
                            agent.StartInfo.WorkingDirectory = config.path;
                            agent.Start();
                        }
                    }
                    else
                    {
                        // If agent.exe does NOT exist, download, THEN start it.
                        await _download.DownloadAgent(config);
                        Process agent = new Process();
                        agent.StartInfo.FileName = agentPath;
                        agent.StartInfo.Arguments = "-service";
                        agent.StartInfo.WorkingDirectory = config.path;
                        agent.Start();
                    }
                } else if (!result && Process.GetProcessesByName("blindspotagent").Length > 0)
                {
                    if(_checkin.IsCampaignComplete(config, campaign))
                    {
                        // Kill process
                        foreach (Process proc in Process.GetProcessesByName("blindspotagent"))
                        {
                            // Stop process immediately.
                            proc.Kill();
                            // Wait for process to exit.
                            proc.WaitForExit();
                            // Release resources used by process.
                            proc.Dispose();
                        }
                    }
                }
                _logger.LogInformation($"Finished task. Waiting {checkinDelay} seconds to repeat.");
                await Task.Delay(checkinDelay, stoppingToken);
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