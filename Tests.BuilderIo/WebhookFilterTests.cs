using Apps.BuilderIo.Actions;
using Apps.BuilderIo.Models.Identifiers;
using Apps.BuilderIo.Models.Requests;
using Apps.BuilderIo.Webhooks;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;
using Tests.BuilderIo.Base;

namespace Tests.BuilderIo;

[TestClass]
public class WebhookFilterTests : TestBase
{
    [TestMethod]
    public async Task Events_UseModelTagAndAuthenticationFilters()
    {
        await using var fixture = new Fixture(Client);
        await fixture.Initialize();
        var actions = new ContentActions(Context);
        var model = await actions.ResolveModel(fixture.ModelId);
        var entry = await actions.Read(model, fixture.ContentId);
        var selector = new ModelIdentifier { ModelId = model.Id };
        var hooks = new ContentWebhooks(Context);
        var filter = new EventFilter { TagField = "tag", Tags = ["keep"] };
        var headers = new Dictionary<string, string> { ["X-Blackbird-Builder"] = ContentWebhookHandler.Token(Creds, model.Id) };
        var published = new WebhookRequest { Headers = headers, Body = JsonConvert.SerializeObject(new { operation = "publish", modelName = model.Name, newValue = entry }) };
        Assert.AreEqual(entry.Id, (await hooks.OnPublished(published, selector, filter)).Result?.ContentId);
        Assert.AreEqual(WebhookRequestType.Preflight, (await hooks.OnPublished(published, selector, new EventFilter { TagField = "tag", Tags = ["unmatched"] })).ReceivedWebhookRequestType);
        var unpublished = new WebhookRequest { Headers = headers, Body = JsonConvert.SerializeObject(new { operation = "unpublish", modelName = model.Name, newValue = entry }) };
        Assert.AreEqual(entry.Id, (await hooks.OnUnpublished(unpublished, selector, filter)).Result?.ContentId);
        var deleted = new WebhookRequest { Headers = headers, Body = JsonConvert.SerializeObject(new { operation = "delete", modelName = model.Name, previousValue = entry }) };
        Assert.AreEqual(entry.Id, (await hooks.OnDeleted(deleted, selector, filter)).Result?.ContentId);
        published.Headers = new Dictionary<string, string>();
        Assert.AreEqual(WebhookRequestType.Preflight, (await hooks.OnPublished(published, selector, filter)).ReceivedWebhookRequestType);
    }
}
