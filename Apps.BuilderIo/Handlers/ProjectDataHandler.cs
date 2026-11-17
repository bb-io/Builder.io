using Blackbird.Applications.Sdk.Common;
using Apps.BuilderIo.Api;
using Apps.BuilderIo.Models.Projects;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.BuilderIo.Handlers;

public class ProjectDataHandler(InvocationContext context) : BaseInvocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        using var client = new ProjectsClient(InvocationContext.AuthenticationCredentialsProviders);
        var projects = await client.Paginate<ProjectResponse>("List projects", "projects", new { search = string.IsNullOrWhiteSpace(context.SearchString) ? null : context.SearchString.Trim() }, 500);
        var duplicateNames = projects.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return projects.Select(p => new DataSourceItem(p.ProjectId, duplicateNames.Contains(p.Name) ? $"{p.Name} ({p.ProjectId})" : p.Name));
    }
}
