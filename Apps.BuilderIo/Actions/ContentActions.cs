using System.Text.RegularExpressions;
using Apps.BuilderIo.Models.Dtos;
using Apps.BuilderIo.Models.Identifiers;
using Apps.BuilderIo.Models.Requests;
using Apps.BuilderIo.Models.Responses;
using Apps.BuilderIo.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.BuilderIo.Actions;

[ActionList("Content")]
public class ContentActions(InvocationContext context) : Invocable(context)
{
    [Action("Search models", Description = "Get the content models accessible in the connected Space.")]
    public async Task<ModelsResponse> SearchModels()
    {
        var space = await Client.Graph<SpaceDto>("query { models { id name kind fields } }");
        return new ModelsResponse { Models = space.Models.Select(m => new ModelResponse { ModelId = m.Id, Name = m.Name, Kind = m.Kind }) };
    }

    [Action("Search locales", Description = "Get configured languages and the default-value option in the connected Space.")]
    public async Task<LocalesResponse> SearchLocales()
    {
        var space = await Client.Graph<SpaceDto>("query { settings }");
        return new LocalesResponse { Locales = SpaceConfiguration.Locales(space) };
    }

    [Action("Search content", Description = "Find content in a model by name, status, update date and tags. Pagination is handled automatically.")]
    public async Task<SearchContentResponse> SearchContent([ActionParameter] SearchContentRequest input)
    {
        var model = await ResolveModel(input.ModelId);
        var request = Client.ContentRequest(model.Name);
        if (!string.IsNullOrWhiteSpace(input.NameContains))
        {
            request.AddQueryParameter("query.name.$regex", Regex.Escape(input.NameContains));
            request.AddQueryParameter("query.name.$options", "i");
        }
        if (input.Status != null)
        {
            if (input.Status is not ("draft" or "published"))
                throw new PluginMisconfigurationException("Select Draft or Published for Status.");
            request.AddQueryParameter("query.published", input.Status);
        }
        if (input.UpdatedAfter.HasValue)
            request.AddQueryParameter("query.lastUpdated.$gt", new DateTimeOffset(input.UpdatedAfter.Value.ToUniversalTime()).ToUnixTimeMilliseconds());
        var tags = input.Tags?.ToList() ?? [];
        if (tags.Count > 0)
        {
            var field = SpaceConfiguration.TagField(model, input.TagField ?? "");
            request.AddQueryParameter($"query.data.{field.Name}.$all", JsonConvert.SerializeObject(tags));
        }
        var entries = await Client.Paginate<ContentDto>(request);
        return new SearchContentResponse { Content = entries.Select(e => Describe(e, model)).ToList() };
    }

    [Action("Get content", Description = "Get the name, status, tags and editor link of a content entry.")]
    public async Task<ContentResponse> GetContent([ActionParameter] ContentIdentifier input)
    {
        var model = await ResolveModel(input.ModelId);
        return Describe(await Read(model, input.ContentId), model);
    }

    [Action("Publish content", Description = "Publish an entire entry, including every inline language. This makes its content available to the website.")]
    public Task<ContentResponse> PublishContent([ActionParameter] ContentIdentifier input) => SetStatus(input, "published");

    [Action("Unpublish content", Description = "Move an entire entry to draft. This affects every inline language and removes it from normal published delivery.")]
    public Task<ContentResponse> UnpublishContent([ActionParameter] ContentIdentifier input) => SetStatus(input, "draft");

    public async Task<ModelDto> ResolveModel(string id)
    {
        Input.Required(id, "Model ID");
        var result = await Client.Graph<SpaceDto>("query($id: String!) { model(id: $id) { id name kind fields } }", new { id });
        return result.Model ?? throw new PluginMisconfigurationException("Model was not found in this Space. Select a model from the connected account.");
    }

    public async Task<ContentDto> Read(ModelDto model, string id)
    {
        var result = await Find(model, Input.Required(id, "Content ID"));
        return result ?? throw new PluginMisconfigurationException("Content was not found in this model. Check the content ID and private-key access to drafts.");
    }

    public async Task<ContentDto?> Find(ModelDto model, string id)
    {
        var request = Client.ContentRequest(model.Name).AddQueryParameter("query.id", id).AddQueryParameter("limit", 1);
        var result = await Client.ExecuteWithErrorHandling<ContentPage<ContentDto>>(request);
        return result.Results.FirstOrDefault();
    }

    public static ContentResponse Describe(ContentDto entry, ModelDto model)
    {
        return new ContentResponse
        {
            ContentId = entry.Id,
            ModelId = model.Id,
            Name = entry.Name,
            Status = entry.Published,
            UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(entry.LastUpdated).UtcDateTime,
            AdminUrl = $"https://builder.io/content/{Uri.EscapeDataString(entry.Id)}",
            Tags = model.Fields.Where(f => f.Type == "tags" && !f.Localized)
                .SelectMany(f => SpaceConfiguration.ReadTags(entry, f.Name)).Distinct(StringComparer.Ordinal).ToList()
        };
    }

    private async Task<ContentResponse> SetStatus(ContentIdentifier input, string status)
    {
        var model = await ResolveModel(input.ModelId);
        var entry = await Read(model, input.ContentId);
        await Client.ExecuteWithErrorHandling(Client.WriteRequest(model.Name, entry.Id, Method.Patch)
            .AddStringBody(JsonConvert.SerializeObject(new { published = status }), DataFormat.Json));
        entry.Published = status;
        return Describe(entry, model);
    }
}
