using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Projects;

public class ProjectsResponse
{
    [Display("Projects")]
    [JsonProperty("projects")]
    public List<ProjectResponse> Projects { get; set; } = [];
}
