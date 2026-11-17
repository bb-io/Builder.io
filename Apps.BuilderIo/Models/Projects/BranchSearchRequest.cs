using Apps.BuilderIo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.BuilderIo.Models.Projects;

public class BranchSearchRequest : ProjectIdentifier
{
    [Display("Search", Description = "Filter branches within the selected project.")]
    public string? Search { get; set; }
}
