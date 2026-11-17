using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.BuilderIo.Models.Dtos;

public class ModelDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("kind")]
    public string Kind { get; set; } = "";

    [JsonProperty("fields")]
    public List<FieldDto> Fields { get; set; } = [];

    [JsonProperty("webhooks")]
    public JArray Webhooks { get; set; } = new();
}
