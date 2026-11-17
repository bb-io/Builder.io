using Apps.BuilderIo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.BuilderIo.Models.Projects;

public class ProjectSearchRequest
{
    [Display("Search", Description = "Filter project names. Leave empty to search all accessible projects.")]
    public string? Search { get; set; }
}
