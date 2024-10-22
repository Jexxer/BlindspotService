using Newtonsoft.Json;
using System.Diagnostics;
using System.ServiceProcess;
namespace App.WindowsService;

public class Checkin
{
    public bool GetPendingC2op(Config config, Campaign campaign)
    {
        using(var client = new HttpClient())
        {
            var postData = new Dictionary<string, string>
            {
                { "API_KEY", $"{config.api_key}" }
            };
            
            var content = new FormUrlEncodedContent(postData);
            try
            {
                var endpoint = new Uri($"{config.base_url}/endpoints/agent/get-pending/{Environment.MachineName}");
                var result = client.PostAsync(endpoint, content).Result;
                var statusCode = (int)result.StatusCode;
                if (statusCode == 200)
                {
                    var json = result.Content.ReadAsStringAsync().Result;
                    var data = JsonConvert.DeserializeObject<CheckinReponse>(json);
                    if (data != null)
                    {
                        if (data.IsUninstall) {
                            // Uninstall this service here
                            
                        }
                        if (data.IsPending)
                        {
                            campaign.CampaignUUID = data.CampaignUUID;
                        }
                        return data.IsPending;
                    }
                }
            }
            catch
            {
                return false;
            }
            
            return false;
        }
    }

    private void UninstallService(string serviceName)
    {
        try
        {
            // Stop the service
            using (ServiceController serviceController = new ServiceController(serviceName))
            {
                if (serviceController.Status != ServiceControllerStatus.Stopped)
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
            }

            // Wait for the service to completely stop
            System.Threading.Thread.Sleep(3000);

            // Delete the service using sc.exe
            Process deleteProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"delete \"{serviceName}\"",
                    Verb = "runas",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            deleteProcess.Start();
            string output = deleteProcess.StandardOutput.ReadToEnd();
            string error = deleteProcess.StandardError.ReadToEnd();
            deleteProcess.WaitForExit();

            if (deleteProcess.ExitCode == 0)
            {
                Console.WriteLine("Service uninstalled successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to uninstall the service. Error: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while trying to uninstall the service: {ex.Message}");
        }
    }

    public bool IsCampaignComplete(Config config, Campaign campaign)
    {
        using (var client = new HttpClient())
        {
            var postData = new Dictionary<string, string>
            {
                { 
                    "API_KEY", $"{config.api_key}"
                },
                {
                    "CAMPAIGN_UUID", $"{campaign.CampaignUUID}"
                }
            };
            var content = new FormUrlEncodedContent(postData);
            try
            {
                var endpoint = new Uri($"{config.base_url}/endpoints/agent/is-campaign-complete");
                var result = client.PostAsync(endpoint, content).Result;
                var statusCode = (int)result.StatusCode;
                if (statusCode == 200)
                {
                    var json = result.Content.ReadAsStringAsync().Result;
                    var data = JsonConvert.DeserializeObject<CampaignResponse>(json);
                    if (data != null)
                    {
                        return data.result;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}

public class CheckinReponse
{
    public bool IsPending { get; set; }
    public string? CampaignUUID { get; set; }
    public bool IsUninstall { get; set; }
}

public class CampaignResponse
{
    public bool result { get; set; }
}