using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Dtos;

public class ContentPage<T>
{
    [JsonProperty("results")]
    public List<T> Results { get; set; } = [];
}
