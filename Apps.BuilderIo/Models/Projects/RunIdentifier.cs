using Apps.BuilderIo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.BuilderIo.Models.Projects;

public class RunIdentifier
{
    [Display("Run ID", Description = "Map the Run ID returned by a create, start or continue task action. Run discovery is not available.")]
    public string RunId { get; set; } = "";
}
