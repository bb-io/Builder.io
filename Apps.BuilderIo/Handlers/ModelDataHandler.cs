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

public class ModelDataHandler(InvocationContext context) : Invocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var result = await new ContentActions(InvocationContext).SearchModels();
        var models = result.Models.Select(m => new { Model = m, Label = ReadableName(m.Name) }).ToList();
        return models.Where(m => string.IsNullOrWhiteSpace(context.SearchString)
                || m.Label.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase)
                || m.Model.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase)
                || m.Model.ModelId.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(m => new DataSourceItem(m.Model.ModelId,
                models.Count(other => string.Equals(other.Label, m.Label, StringComparison.OrdinalIgnoreCase)) > 1
                    ? $"{m.Label} ({m.Model.Name})" : m.Label));
    }

    private static string ReadableName(string name)
    {
        var label = Regex.Replace(name, "[-_\\s]+", " ").Trim();
        return label.Length == 0 ? "Unnamed model" : char.ToUpperInvariant(label[0]) + label[1..];
    }
}
