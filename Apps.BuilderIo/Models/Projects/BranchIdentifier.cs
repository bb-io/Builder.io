using Apps.BuilderIo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.BuilderIo.Models.Projects;

public class BranchIdentifier : ProjectIdentifier
{
    [Display("Branch name", Description = "Choose an existing branch in the selected project, or enter/map its full Git name.")]
    [DataSource(typeof(BranchDataHandler))]
    public string BranchName { get; set; } = "";
}
