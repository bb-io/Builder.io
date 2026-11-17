using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Projects;

public class ProjectResponse
{
    [Display("Project ID")]
    [JsonProperty("projectId")]
    public string ProjectId { get; set; } = "";

    [Display("Name")]
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [Display("Project URL")]
    [JsonProperty("url")]
    public string Url { get; set; } = "";

    [Display("Main branch name")]
    [JsonProperty("mainBranchName")]
    public string? MainBranchName { get; set; }

    [Display("Creator ID")]
    [JsonProperty("createdBy")]
    public string? CreatedBy { get; set; }

    [Display("Creator name")]
    [JsonProperty("createdByName")]
    public string? CreatedByName { get; set; }
}
