using Apps.BuilderIo.Actions;
using Apps.BuilderIo.Models.Projects;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.BuilderIo.Handlers;

public class BranchDataHandler(InvocationContext context, [ActionParameter] ProjectIdentifier parent) : BaseInvocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parent.ProjectId))
            return [];
        var result = await new ProjectActions(InvocationContext).ReadBranches(new BranchSearchRequest { ProjectId = parent.ProjectId, Search = context.SearchString }, 500);
        return result.Branches.Select(b => new DataSourceItem(b.Name, string.IsNullOrWhiteSpace(b.FriendlyName) || b.FriendlyName == b.Name ? b.Name : $"{b.FriendlyName} ({b.Name})"));
    }
}
