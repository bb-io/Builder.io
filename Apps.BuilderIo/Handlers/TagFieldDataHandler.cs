using Apps.BuilderIo.Actions;
using Apps.BuilderIo.Models.Dtos;
using Apps.BuilderIo.Models.Identifiers;
using Apps.BuilderIo.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;
using System.Text.RegularExpressions;

namespace Apps.BuilderIo.Handlers;

public class TagFieldDataHandler(InvocationContext context, [ActionParameter] ModelIdentifier parent) : Invocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parent.ModelId))
            return [];
        var model = await new ContentActions(InvocationContext).ResolveModel(parent.ModelId);
        return model.Fields.Where(f => f.Type == "tags" && !f.Localized)
            .Where(f => string.IsNullOrWhiteSpace(context.SearchString) || (f.FriendlyName ?? f.Name).Contains(context.SearchString, StringComparison.OrdinalIgnoreCase) || f.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(f => new DataSourceItem(f.Name, $"{f.FriendlyName ?? f.Name} ({f.Name})"));
    }
}
