using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Dtos;

public class GraphError
{
    [JsonProperty("message")]
    public string Message { get; set; } = "";
}
