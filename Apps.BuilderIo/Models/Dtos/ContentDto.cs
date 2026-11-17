using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.BuilderIo.Models.Dtos;

public class ContentDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("published")]
    public string Published { get; set; } = "draft";

    [JsonProperty("lastUpdated")]
    public long LastUpdated { get; set; }

    [JsonProperty("data")]
    public JObject Data { get; set; } = new();

    [JsonProperty("meta")]
    public JObject Meta { get; set; } = new();

    [JsonProperty("query")]
    public JArray Query { get; set; } = new();
}
