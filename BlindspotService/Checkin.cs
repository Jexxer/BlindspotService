using Newtonsoft.Json;
namespace App.WindowsService;

public class Checkin
{
    public string GetPendingC2op()
    {
        using(var client = new HttpClient())
        {
            var endpoint = new Uri("http://127.0.0.1:8888/endpoints/agent/get-pending/VjPcll65.h6WAszekpKiKRrQimHfYrt8zByXJpRtI");
            var result = client.GetAsync(endpoint).Result;
            var json = result.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"first (json): {json}");
            var data = JsonConvert.DeserializeObject<JSONResponse>(json);
            if(data != null)
            {
                Console.WriteLine($"second (data): {data.isPending}");
            }
            return json
        }
    }
}

public class JSONResponse
{
    public bool isPending { get; set; }
}

