using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.BuilderIo.Handlers.Static;

public class ModeDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new("field", "Field-level (default)"),
        new("entry", "Whole-entry localization")
    ];
}
