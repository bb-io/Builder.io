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

public class TagDataHandler(InvocationContext context, [ActionParameter] TagFieldIdentifier parent) : Invocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parent.ModelId) || string.IsNullOrWhiteSpace(parent.TagField))
            return [];
        var result = await new TagActions(InvocationContext).SearchTags(parent);
        return result.Tags.Where(t => string.IsNullOrWhiteSpace(context.SearchString) || t.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(t => new DataSourceItem(t, t));
    }
}
