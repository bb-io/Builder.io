using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.BuilderIo.Models.Dtos;

public class FieldDto
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("friendlyName")]
    public string? FriendlyName { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; } = "";

    [JsonProperty("localized")]
    public bool Localized { get; set; }

    [JsonProperty("subFields")]
    public List<FieldDto> SubFields { get; set; } = [];

    [JsonProperty("options")]
    public JObject Options { get; set; } = new();
}
