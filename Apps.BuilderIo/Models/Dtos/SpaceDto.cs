using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.BuilderIo.Models.Dtos;

public class SpaceDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("settings")]
    public JObject Settings { get; set; } = new();

    [JsonProperty("models")]
    public List<ModelDto> Models { get; set; } = [];

    [JsonProperty("model")]
    public ModelDto? Model { get; set; }
}
