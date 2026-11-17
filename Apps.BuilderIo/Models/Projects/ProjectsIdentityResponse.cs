using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.BuilderIo.Models.Projects;

public class ProjectsIdentityResponse
{
    [Display("User ID")]
    [JsonProperty("userId")]
    public string UserId { get; set; } = "";

    [Display("User name")]
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [Display("Email")]
    [JsonProperty("email")]
    public string Email { get; set; } = "";

    [Display("Organization")]
    [JsonProperty("org")]
    public OrganizationResponse Organization { get; set; } = new();
}
