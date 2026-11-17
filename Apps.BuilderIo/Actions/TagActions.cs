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

[ActionList("Tags")]
public class TagActions(InvocationContext context) : Invocable(context)
{
    [Action("Search tags", Description = "Find existing tags in a selected model field across accessible content entries.")]
    public async Task<TagsResponse> SearchTags([ActionParameter] TagFieldIdentifier input)
    {
        var model = await new ContentActions(InvocationContext).ResolveModel(input.ModelId);
        var field = SpaceConfiguration.TagField(model, input.TagField);
        var entries = await Client.Paginate<ContentDto>(Client.ContentRequest(model.Name));
        return new TagsResponse { Tags = entries.SelectMany(e => SpaceConfiguration.ReadTags(e, field.Name)).Distinct(StringComparer.Ordinal).OrderBy(x => x).ToList() };
    }

    [Action("Get content tags", Description = "Read tags from a selected field on one content entry.")]
    public async Task<TagsResponse> GetContentTags([ActionParameter] ContentTagsRequest input)
    {
        var actions = new ContentActions(InvocationContext);
        var model = await actions.ResolveModel(input.ModelId);
        var field = SpaceConfiguration.TagField(model, input.TagField);
        return new TagsResponse { Tags = SpaceConfiguration.ReadTags(await actions.Read(model, input.ContentId), field.Name) };
    }

    [Action("Add content tags", Description = "Add tags without duplicating or removing existing tags. Does not publish content.")]
    public Task<TagsResponse> AddContentTags([ActionParameter] ChangeTagsRequest input) => Change(input, true);

    [Action("Remove content tags", Description = "Remove selected tags while preserving other tags. Does not publish content.")]
    public Task<TagsResponse> RemoveContentTags([ActionParameter] ChangeTagsRequest input) => Change(input, false);

    private async Task<TagsResponse> Change(ChangeTagsRequest input, bool add)
    {
        var values = input.Tags.Select(x => Input.Required(x, "Tags")).Distinct(StringComparer.Ordinal).ToList();
        if (values.Count == 0)
            throw new PluginMisconfigurationException("Provide at least one tag.");
        var actions = new ContentActions(InvocationContext);
        var model = await actions.ResolveModel(input.ModelId);
        var field = SpaceConfiguration.TagField(model, input.TagField);
        var entry = await actions.Read(model, input.ContentId);
        var old = SpaceConfiguration.ReadTags(entry, field.Name);
        var tags = add ? old.Union(values, StringComparer.Ordinal).ToList() : old.Except(values, StringComparer.Ordinal).ToList();
        if (tags.SequenceEqual(old))
            return new TagsResponse { Tags = tags };
        if (entry.Published != "draft")
            throw new PluginMisconfigurationException("This content is not a draft. Explicitly unpublish it before modifying tags, then publish after review.");
        var current = await actions.Read(model, entry.Id);
        if (current.LastUpdated != entry.LastUpdated)
            throw new PluginMisconfigurationException("Content changed while tags were being updated. Retry with the latest content.");
        current.Data[field.Name] = new JArray(tags);
        await Client.ExecuteWithErrorHandling(Client.WriteRequest(model.Name, entry.Id, Method.Patch)
            .AddQueryParameter("triggerWebhooks", "false")
            .AddStringBody(JsonConvert.SerializeObject(new { data = current.Data }), DataFormat.Json));
        return new TagsResponse { Tags = tags };
    }
}
