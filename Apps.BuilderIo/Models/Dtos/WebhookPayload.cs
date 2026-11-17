using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Dtos;

public class WebhookPayload
{
    [JsonProperty("newValue")]
    public ContentDto? NewValue { get; set; }

    [JsonProperty("previousValue")]
    public ContentDto? PreviousValue { get; set; }

    [JsonProperty("modelName")]
    public string ModelName { get; set; } = "";

    [JsonProperty("operation")]
    public string Operation { get; set; } = "";
}
