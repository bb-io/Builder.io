using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Projects;

public class BranchesResponse
{
    [Display("Branches")]
    [JsonProperty("branches")]
    public List<BranchResponse> Branches { get; set; } = [];
}
