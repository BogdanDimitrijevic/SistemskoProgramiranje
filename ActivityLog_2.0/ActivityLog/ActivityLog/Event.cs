

using Newtonsoft.Json;

public class Event
{
    [JsonProperty("type")]
    public string EventType { get; set; }

    [JsonProperty("created_at")]
    public DateTime DateCreated { get; set; }

    [JsonProperty("actor")]
    public Author Auth { get; set; }
}


