using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Projects;

public class OrganizationResponse
{
    [Display("Organization ID")]
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [Display("Organization name")]
    [JsonProperty("name")]
    public string Name { get; set; } = "";
}
