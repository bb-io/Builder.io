using Apps.BuilderIo.Models.Dtos;
using Apps.BuilderIo.Models.Identifiers;
using Apps.BuilderIo.Webhooks;
using Newtonsoft.Json.Linq;
using Tests.BuilderIo.Base;

namespace Tests.BuilderIo;

[TestClass]
public class WebhookTests : TestBase
{
    [TestMethod]
    public async Task SubscribeAndUnsubscribe_PreservesExistingWebhooks()
    {
        await using var fixture = new Fixture(Client);
        await fixture.Initialize();
        var before = await Client.Graph<SpaceDto>("query { settings }");
        var original = before.Settings["webhooks"]?.DeepClone() ?? new JArray();
        var handler = new ContentWebhookHandler(Context, new ModelIdentifier { ModelId = fixture.ModelId });
        var values = new Dictionary<string, string> { ["payloadUrl"] = fixture.CallbackUrl };
        try
        {
            await handler.SubscribeAsync(Creds, values);
            var after = await Client.Graph<SpaceDto>("query { settings }");
            Assert.IsTrue(after.Settings["webhooks"]!.Any(x => x["url"]?.ToString() == fixture.CallbackUrl));
            await handler.SubscribeAsync(Creds, values);
            var duplicate = await Client.Graph<SpaceDto>("query { settings }");
            Assert.AreEqual(1, duplicate.Settings["webhooks"]!.Count(x => x["url"]?.ToString() == fixture.CallbackUrl));
        }
        finally
        {
            await handler.UnsubscribeAsync(Creds, values);
        }
        var final = await Client.Graph<SpaceDto>("query { settings }");
        Assert.IsTrue(JToken.DeepEquals(original, final.Settings["webhooks"] ?? new JArray()));
    }
}
