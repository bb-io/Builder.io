using Apps.BuilderIo.Handlers;
using Apps.BuilderIo.Models.Identifiers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Tests.BuilderIo.Base;

namespace Tests.BuilderIo;

[TestClass]
public class HandlerTests : TestBase
{
    [TestMethod]
    public async Task Dropdowns_AreContextualAndSearchable()
    {
        await using var fixture = new Fixture(Client);
        await fixture.Initialize();
        var search = new DataSourceContext { SearchString = fixture.ModelName };
        var models = await new ModelDataHandler(Context).GetDataAsync(search, CancellationToken.None);
        Assert.IsTrue(models.Any(m => m.Value == fixture.ModelId));
        var initialEntries = await new ContentDataHandler(Context, new ModelIdentifier { ModelId = fixture.ModelId }).GetDataAsync(new DataSourceContext(), CancellationToken.None);
        Assert.IsTrue(initialEntries.Any(e => e.Value == fixture.ContentId));
        var entries = await new ContentDataHandler(Context, new ModelIdentifier { ModelId = fixture.ModelId }).GetDataAsync(new DataSourceContext { SearchString = "round-trip" }, CancellationToken.None);
        Assert.IsTrue(entries.Any(e => e.Value == fixture.ContentId));
        var fields = await new TagFieldDataHandler(Context, new ModelIdentifier { ModelId = fixture.ModelId }).GetDataAsync(new DataSourceContext(), CancellationToken.None);
        Assert.IsTrue(fields.Any(f => f.Value == "tag"));
        var tags = await new TagDataHandler(Context, new TagFieldIdentifier { ModelId = fixture.ModelId, TagField = "tag" }).GetDataAsync(new DataSourceContext { SearchString = "keep" }, CancellationToken.None);
        Assert.AreEqual("keep", tags.Single().Value);
        var locales = await new LocaleDataHandler(Context).GetDataAsync(new DataSourceContext(), CancellationToken.None);
        Assert.IsTrue(locales.Any());
        var empty = await new ContentDataHandler(Context, new ModelIdentifier()).GetDataAsync(new DataSourceContext(), CancellationToken.None);
        Assert.IsFalse(empty.Any());
    }
}
