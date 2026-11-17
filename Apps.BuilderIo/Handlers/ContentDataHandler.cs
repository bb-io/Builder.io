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

public class ContentDataHandler(InvocationContext context, [ActionParameter] ModelIdentifier parent) : Invocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parent.ModelId))
            return [];
        var model = await new ContentActions(InvocationContext).ResolveModel(parent.ModelId);
        var request = Client.ContentRequest(model.Name);
        if (!string.IsNullOrWhiteSpace(context.SearchString))
        {
            request.AddQueryParameter("query.name.$regex", Regex.Escape(context.SearchString));
            request.AddQueryParameter("query.name.$options", "i");
        }
        var entries = await Client.Paginate<ContentDto>(request, 500);
        return entries.Select(e => new DataSourceItem(e.Id, $"{e.Name} ({e.Published}; {e.Id})"));
    }
}
