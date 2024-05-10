using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;




public class Program
{

    public static readonly HttpClient _httpClient = new();

    public static DateTime LastCleanupTime = DateTime.Now;

    public static void Main(string[] args)
    {
        var baseDir = Extensions.ProjectBase();
        if (baseDir != null)
        {
            var path = Path.Combine(baseDir, ".env");
            Env.Insert(path);
        }

        ConfigureHttpClient();


        var listen = new HttpListener();
        listen.Prefixes
                .Add("http://localhost:5050/");


        listen.Start();

        Console.WriteLine("Listening:");



        while (true)
        {
            ThreadPool.QueueUserWorkItem(Serve, listen.GetContext());
        }
    }

    private static void ConfigureHttpClient()
    {

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GitHub_API");
        var ghToken = Environment.GetEnvironmentVariable("GH_TOKEN");
        if (!string.IsNullOrEmpty(ghToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ghToken);
        }
    }
    private static List<Event> Fetch(string apiUrl)
    {

        var cachedEvents = Cache.GetFromCache(apiUrl);
        if (cachedEvents != null)
        {
            return cachedEvents;
        }


        var res = _httpClient.GetAsync(apiUrl).Result;
        if (!res.IsSuccessStatusCode)
            throw new Exception($"ERROR: {res.StatusCode}");

        var content = res.Content.ReadAsStringAsync().Result;
        var events = JsonConvert.DeserializeObject<List<Event>>(content);


        Cache.AddToCache(apiUrl, events);

        return events;
    }
    private static void Serve(object? reqstate)
    {
        if (reqstate == null)
            return;

        var context = (HttpListenerContext)reqstate;
        var response = context.Response;
        try
        {
            Stopwatch sw = new();
            sw.Start();


            var query = context.Request.Url?.Query;


            if (!string.IsNullOrEmpty(query))
            {
                var vars = query.Substring(1)?.Split("&");
                if (vars == null || vars.Length != 2)
                {
                    throw new Exception("Parameter error!");
                }

                var owner = vars[0].Split("=")[1];
                var repo = vars[1].Split("=")[1];
                var key = $"{owner}/{repo}";
                var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/events";
                var events = Fetch(apiUrl);
                var responseObject = new
                {
                    Owner = owner,
                    Repo = repo,
                    Events = events
                };

                var responseJson = JsonConvert.SerializeObject(responseObject);
                var responseByteArray = Encoding.UTF8.GetBytes(responseJson);
                
                
                response.ContentLength64 = responseByteArray.Length;
                response.ContentType = "application/json";
                response.OutputStream.Write(responseByteArray, 0, responseByteArray.Length);

                sw.Stop();



                Console.WriteLine($"Request of repo '{repo}' loaded in {sw.ElapsedMilliseconds} ms.");

            }
        }

        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");


            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.StatusDescription = e.Message;
        }
    }


}