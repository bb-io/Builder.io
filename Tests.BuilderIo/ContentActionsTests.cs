using Apps.BuilderIo.Actions;
using Apps.BuilderIo.Models.Identifiers;
using Apps.BuilderIo.Models.Requests;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Tests.BuilderIo.Base;
using Newtonsoft.Json;
using RestSharp;

namespace Tests.BuilderIo;

[TestClass]
public class ContentActionsTests : TestBase
{
    [TestMethod]
    public async Task SearchModels_ReturnsAccessibleModels()
    {
        var result = await new ContentActions(Context).SearchModels();
        Assert.IsNotNull(result.Models);
    }

    [TestMethod]
    public async Task SearchLocales_ReturnsConfiguredLocales()
    {
        var result = await new ContentActions(Context).SearchLocales();
        CollectionAssert.Contains(result.Locales.ToList(), "Default");
    }

    [TestMethod]
    public async Task SearchAndGetContent_ReturnsCreatedEntryAndTags()
    {
        await using var fixture = new Fixture(Client);
        await fixture.Initialize();
        var actions = new ContentActions(Context);
        var result = await actions.SearchContent(new SearchContentRequest { ModelId = fixture.ModelId, NameContains = "round-trip", TagField = "tag", Tags = ["keep"] });
        Assert.IsTrue(result.Content.Any(e => e.ContentId == fixture.ContentId));
        var entry = await actions.GetContent(new ContentIdentifier { ModelId = fixture.ModelId, ContentId = fixture.ContentId });
        Assert.AreEqual("draft", entry.Status);
        CollectionAssert.Contains(entry.Tags.ToList(), "keep");
    }

    [TestMethod]
    public async Task PublishAndUnpublishContent_ChangesStatus()
    {
        await using var fixture = new Fixture(Client);
        await fixture.Initialize();
        var actions = new ContentActions(Context);
        var input = new ContentIdentifier { ModelId = fixture.ModelId, ContentId = fixture.ContentId };
        await actions.PublishContent(input);
        Assert.AreEqual("published", (await actions.GetContent(input)).Status);
        await actions.UnpublishContent(input);
        Assert.AreEqual("draft", (await actions.GetContent(input)).Status);
    }

    [TestMethod]
    public async Task SearchContent_AcrossPages_ReturnsEveryUniqueEntry()
    {
        await using var fixture = new Fixture(Client);
        await fixture.Initialize();
        for (var i = 0; i < 105; i++)
        {
            await Client.ExecuteWithErrorHandling(Client.WriteRequest(fixture.ModelName, Guid.NewGuid().ToString("N"), Method.Put)
                .AddStringBody(JsonConvert.SerializeObject(new { name = "Pagination entry " + i, published = "draft", data = new { title = "Pagination" } }), DataFormat.Json));
        }
        var entries = (await new ContentActions(Context).SearchContent(new SearchContentRequest { ModelId = fixture.ModelId })).Content.ToList();
        Assert.AreEqual(106, entries.Count);
        Assert.AreEqual(106, entries.Select(e => e.ContentId).Distinct().Count());
    }

    [TestMethod]
    public async Task GetContent_UnknownId_ExplainsFailure()
    {
        await using var fixture = new Fixture(Client);
        await fixture.Initialize();
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() => new ContentActions(Context).GetContent(new ContentIdentifier { ModelId = fixture.ModelId, ContentId = Guid.NewGuid().ToString("N") }));
    }
}
