using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Projects;

public class AgentRunResponse
{
    [Display("Run ID")]
    [JsonProperty("runId")]
    public string RunId { get; set; } = "";

    [Display("Status")]
    [JsonProperty("status")]
    public string Status { get; set; } = "";

    [Display("Project ID")]
    [JsonProperty("projectId")]
    public string? ProjectId { get; set; }

    [Display("Branch name")]
    [JsonProperty("branchName")]
    public string? BranchName { get; set; }

    [Display("Preview URL")]
    [JsonProperty("url")]
    public string? Url { get; set; }

    [Display("Prompt")]
    [JsonProperty("prompt")]
    public string Prompt { get; set; } = "";

    [Display("Idempotency key")]
    [JsonProperty("idempotencyKey")]
    public string? IdempotencyKey { get; set; }

    [Display("Created at")]
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [Display("Updated at")]
    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [Display("Error")]
    [JsonProperty("error")]
    public AgentError? Error { get; set; }
}
