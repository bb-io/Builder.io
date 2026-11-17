using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Dtos;

public class GraphResponse<T>
{
    [JsonProperty("data")]
    public T Data { get; set; } = default!;

    [JsonProperty("errors")]
    public List<GraphError> Errors { get; set; } = [];
}
