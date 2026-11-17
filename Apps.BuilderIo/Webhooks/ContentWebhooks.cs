using System.Net;
using System.Security.Cryptography;
using System.Text;
using Apps.BuilderIo.Actions;
using Apps.BuilderIo.Models.Dtos;
using Apps.BuilderIo.Models.Identifiers;
using Apps.BuilderIo.Models.Requests;
using Apps.BuilderIo.Models.Responses;
using Apps.BuilderIo.Utils;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Blackbird.Applications.SDK.Blueprints;
using Newtonsoft.Json;

namespace Apps.BuilderIo.Webhooks;

[WebhookList("Content")]
public class ContentWebhooks(InvocationContext context) : Invocable(context)
{
    [BlueprintEventDefinition(BlueprintEvent.ContentCreatedOrUpdated)]
    [Webhook("On content published", typeof(ContentWebhookHandler), Description = "Start when an entry is published or its scheduled start occurs. Routine draft edits do not trigger this event.")]
    public Task<WebhookResponse<ContentResponse>> OnPublished(WebhookRequest request, [WebhookParameter(true)] ModelIdentifier model, [WebhookParameter] EventFilter filter)
        => Handle(request, model, filter, ["publish", "scheduledStart"]);

    [Webhook("On content unpublished", typeof(ContentWebhookHandler), Description = "Start when published content is unpublished, archived, or reaches its scheduled end.")]
    public Task<WebhookResponse<ContentResponse>> OnUnpublished(WebhookRequest request, [WebhookParameter(true)] ModelIdentifier model, [WebhookParameter] EventFilter filter)
        => Handle(request, model, filter, ["unpublish", "archive", "scheduledEnd"]);

    [Webhook("On content deleted", typeof(ContentWebhookHandler), Description = "Start when a content deletion webhook is delivered. Uses the previous content values for tag filters.")]
    public Task<WebhookResponse<ContentResponse>> OnDeleted(WebhookRequest request, [WebhookParameter(true)] ModelIdentifier model, [WebhookParameter] EventFilter filter)
        => Handle(request, model, filter, ["delete"]);

    private async Task<WebhookResponse<ContentResponse>> Handle(WebhookRequest request, ModelIdentifier model, EventFilter filter, string[] operations)
    {
        var supplied = request.Headers?.FirstOrDefault(h => h.Key.Equals("X-Blackbird-Builder", StringComparison.OrdinalIgnoreCase)).Value ?? "";
        var expected = ContentWebhookHandler.Token(InvocationContext.AuthenticationCredentialsProviders, model.ModelId);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(expected)))
            return Preflight();
        WebhookPayload payload;
        try
        {
            payload = JsonConvert.DeserializeObject<WebhookPayload>(request.Body?.ToString() ?? "")
                ?? throw new JsonException();
        }
        catch (JsonException)
        {
            throw new PluginMisconfigurationException("The webhook payload is invalid. Enable full payloads for this webhook in Builder.io.");
        }
        if (!operations.Contains(payload.Operation))
            return Preflight();
        var selectedModel = await new ContentActions(InvocationContext).ResolveModel(model.ModelId);
        if (payload.ModelName != selectedModel.Name)
            return Preflight();
        var entry = payload.NewValue ?? payload.PreviousValue;
        if (entry == null || (filter.ContentId != null && filter.ContentId != entry.Id))
            return Preflight();
        var tags = filter.Tags?.ToList() ?? [];
        if (tags.Count > 0)
        {
            var field = SpaceConfiguration.TagField(selectedModel, filter.TagField ?? "");
            var actual = SpaceConfiguration.ReadTags(entry, field.Name);
            if (!tags.All(t => actual.Contains(t, StringComparer.Ordinal)))
                return Preflight();
        }
        return new WebhookResponse<ContentResponse>
        {
            HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK),
            Result = ContentActions.Describe(entry, selectedModel)
        };
    }

    private static WebhookResponse<ContentResponse> Preflight() => new()
    {
        HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK),
        ReceivedWebhookRequestType = WebhookRequestType.Preflight,
        Result = null
    };
}
