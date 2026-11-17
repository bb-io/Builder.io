using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Projects;

public class PromptHistoryResponse
{
    [Display("Project ID")]
    [JsonProperty("projectId")]
    public string ProjectId { get; set; } = "";

    [Display("Branch name")]
    [JsonProperty("branchName")]
    public string BranchName { get; set; } = "";

    [Display("Prompts")]
    [JsonProperty("messages")]
    public List<PromptResponse> Messages { get; set; } = [];
}
