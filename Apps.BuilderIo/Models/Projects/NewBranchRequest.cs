using Apps.BuilderIo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.BuilderIo.Models.Projects;

public class NewBranchRequest
{
    [Display("New branch name", Description = "Optional full Git name for a new branch. When omitted, Builder generates the name. Do not enter an existing branch.")]
    public string? NewBranchName { get; set; }
}
