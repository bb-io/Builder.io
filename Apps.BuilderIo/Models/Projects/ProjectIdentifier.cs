using Apps.BuilderIo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.BuilderIo.Models.Projects;

public class ProjectIdentifier
{
    [Display("Project ID", Description = "Select a Builder Code project from this connection's organization, or enter/map its ID.")]
    [DataSource(typeof(ProjectDataHandler))]
    public string ProjectId { get; set; } = "";
}
