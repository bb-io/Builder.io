using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Projects;

public class PromptResponse
{
    [Display("User prompt")]
    [JsonProperty("userPrompt")]
    public string UserPrompt { get; set; } = "";

    [Display("Title")]
    [JsonProperty("title")]
    public string? Title { get; set; }
}
