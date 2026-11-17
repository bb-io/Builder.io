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

public class LocaleDataHandler(InvocationContext context) : Invocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var result = await new ContentActions(InvocationContext).SearchLocales();
        return result.Locales.Where(l => string.IsNullOrWhiteSpace(context.SearchString) || l.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(l => new DataSourceItem(l, l == "Default" ? "Default value (explicit selection)" : l));
    }
}
