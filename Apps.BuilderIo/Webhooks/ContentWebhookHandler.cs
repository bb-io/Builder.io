using System.Security.Cryptography;
using System.Text;
using Apps.BuilderIo.Api;
using Apps.BuilderIo.Constants;
using Apps.BuilderIo.Models.Dtos;
using Apps.BuilderIo.Models.Identifiers;
using Apps.BuilderIo.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Apps.BuilderIo.Webhooks;

public class ContentWebhookHandler(InvocationContext context, [WebhookParameter(true)] ModelIdentifier model)
    : BaseInvocable(context), IWebhookEventHandler
{
    public async Task SubscribeAsync(IEnumerable<AuthenticationCredentialsProvider> credentials, Dictionary<string, string> values)
    {
        var client = new BuilderIoClient(credentials);
        Input.Required(model.ModelId, "Model ID");
        var selected = await client.Graph<SpaceDto>("query($id: String!) { model(id: $id) { id name kind fields } }", new { id = model.ModelId });
        if (selected.Model == null)
            throw new PluginMisconfigurationException("Select a model accessible through this connection before subscribing.");
        var response = await client.Graph<SpaceDto>("query { settings }");
        var hooks = (JArray?)response.Settings["webhooks"]?.DeepClone() ?? new JArray();
        var url = values["payloadUrl"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            throw new PluginMisconfigurationException("The webhook callback must be a valid HTTPS URL.");
        if (hooks.Any(x => x["url"]?.Value<string>() == url))
            return;
        hooks.Add(new JObject
        {
            ["url"] = url,
            ["disablePayload"] = false,
            ["customHeaders"] = new JArray(new JObject { ["name"] = "X-Blackbird-Builder", ["value"] = Token(credentials, model.ModelId) })
        });
        await Save(client, hooks);
    }

    public async Task UnsubscribeAsync(IEnumerable<AuthenticationCredentialsProvider> credentials, Dictionary<string, string> values)
    {
        var client = new BuilderIoClient(credentials);
        var response = await client.Graph<SpaceDto>("query { settings }");
        var hooks = (JArray?)response.Settings["webhooks"]?.DeepClone() ?? new JArray();
        var matches = hooks.Where(x => x["url"]?.Value<string>() == values["payloadUrl"]).ToList();
        if (matches.Count == 0)
            return;
        foreach (var match in matches)
            match.Remove();
        await Save(client, hooks);
    }

    public static string Token(IEnumerable<AuthenticationCredentialsProvider> credentials, string modelId)
    {
        var key = credentials.First(x => x.KeyName == CredsNames.PrivateKey).Value;
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes("Blackbird.BuilderIo.webhook:" + modelId)));
    }

    private static string Literal(JToken value)
    {
        if (value is JObject obj)
        {
            if (obj.Properties().Any(p => !Regex.IsMatch(p.Name, "^[_A-Za-z][_0-9A-Za-z]*$")))
                throw new PluginMisconfigurationException("An existing webhook setting cannot be represented safely. Configure this webhook manually or contact support.");
            return "{" + string.Join(",", obj.Properties().Select(p => p.Name + ":" + Literal(p.Value))) + "}";
        }
        if (value is JArray array)
            return "[" + string.Join(",", array.Select(Literal)) + "]";
        return value.ToString(Formatting.None);
    }

    private static async Task Save(BuilderIoClient client, JArray hooks)
    {
        await client.Graph<object>("mutation { updateWebhooks(webhooks: " + Literal(hooks) + ") }");
    }
}
