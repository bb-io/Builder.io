using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Projects;

public class AgentError
{
    [Display("Error code")]
    [JsonProperty("code")]
    public string Code { get; set; } = "";

    [Display("Error message")]
    [JsonProperty("message")]
    public string Message { get; set; } = "";
}
