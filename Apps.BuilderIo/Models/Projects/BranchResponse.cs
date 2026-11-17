using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Projects;

public class BranchResponse
{
    [Display("Branch name")]
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [Display("Project ID")]
    [JsonProperty("projectId")]
    public string ProjectId { get; set; } = "";

    [Display("Branch URL")]
    [JsonProperty("url")]
    public string Url { get; set; } = "";

    [Display("Friendly name")]
    [JsonProperty("friendlyName")]
    public string? FriendlyName { get; set; }

    [Display("Description")]
    [JsonProperty("description")]
    public string? Description { get; set; }

    [Display("Category")]
    [JsonProperty("category")]
    public string? Category { get; set; }

    [Display("State")]
    [JsonProperty("state")]
    public string? State { get; set; }

    [Display("Pull request status")]
    [JsonProperty("prStatus")]
    public string? PrStatus { get; set; }

    [Display("Pull request URL")]
    [JsonProperty("prUrl")]
    public string? PrUrl { get; set; }

    [Display("Agent state")]
    [JsonProperty("agentState")]
    public string? AgentState { get; set; }

    [Display("Has unpushed changes")]
    [JsonProperty("hasUnpushedChanges")]
    public bool? HasUnpushedChanges { get; set; }
}
