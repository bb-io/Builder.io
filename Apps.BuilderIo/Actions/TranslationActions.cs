using System.Text;
using Apps.BuilderIo.Models.Dtos;
using Apps.BuilderIo.Models.Requests;
using Apps.BuilderIo.Models.Responses;
using Apps.BuilderIo.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Coders;
using Blackbird.Filters.Shared;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.BuilderIo.Actions;

[ActionList("Localization")]
public class TranslationActions(InvocationContext context, IFileManagementClient files) : Invocable(context)
{
    [BlueprintActionDefinition(BlueprintAction.DownloadContent)]
    [Action("Download content", Description = "Export localizable text as a self-describing file for translation, preserving field mappings and layout metadata.")]
    public async Task<ContentFileResponse> DownloadContent([ActionParameter] DownloadContentRequest input)
    {
        var space = await Client.Graph<SpaceDto>("query { settings }");
        SpaceConfiguration.ValidateLocale(space, input.Locale);
        var actions = new ContentActions(InvocationContext);
        var model = await actions.ResolveModel(input.ModelId);
        var entry = await actions.Read(model, input.ContentId);
        var manifest = ContentRoundtrip.Extract(entry, model, Client.SpaceId, input.Locale, input.Mode ?? "field");
        var name = ContentRoundtrip.FileName(entry.Name, input.Locale);
        var html = ContentRoundtrip.Export(manifest, entry.Name);
        var coded = new HtmlCoder().Deserialize(html, name);
        coded.SystemReference = Reference(manifest, entry);
        coded.Language = input.Locale;
        using var stream = coded.ToStream();
        return new ContentFileResponse
        {
            Content = await files.UploadAsync(stream, "text/html", name),
            RootEntryId = manifest.RootId,
            ModelId = model.Id
        };
    }

    [BlueprintActionDefinition(BlueprintAction.UploadContent)]
    [Action("Upload content", Description = "Import translated HTML or XLIFF into a draft. Update inline languages or create/update a separate locale-targeted entry. Never publishes automatically.")]
    public async Task<ContentFileResponse> UploadContent([ActionParameter] UploadContentRequest input)
    {
        if (input.Content == null)
            throw new PluginMisconfigurationException("Provide a translation file.");
        var extension = Path.GetExtension(input.Content.Name).ToLowerInvariant();
        if (extension is not (".html" or ".xlf" or ".xliff"))
            throw new PluginMisconfigurationException("Use an HTML, XLF or XLIFF file exported from Download content.");
        var space = await Client.Graph<SpaceDto>("query { settings }");
        SpaceConfiguration.ValidateLocale(space, input.Locale);
        using var downloaded = await files.DownloadAsync(input.Content);
        using var buffered = new MemoryStream();
        var buffer = new byte[81920];
        int count;
        while ((count = await downloaded.ReadAsync(buffer)) > 0)
        {
            if (buffered.Length + count > 20 * 1024 * 1024)
                throw new PluginMisconfigurationException("This translation file exceeds the 20 MB limit. Split the content into smaller entries.");
            await buffered.WriteAsync(buffer.AsMemory(0, count));
        }
        buffered.Position = 0;
        TransformationLoadResult loaded;
        try
        {
            loaded = Transformation.Load(buffered, input.Content.Name, input.Content.ContentType);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new PluginMisconfigurationException("The translation file could not be read. Export a valid HTML or Blackbird-interoperable XLIFF file and retry.");
        }
        string html;
        var targetContent = loaded.Success ? loaded.Target() : null;
        if (targetContent?.Success == true)
        {
            using var targetStream = targetContent.Value.ToStream();
            using var reader = new StreamReader(targetStream);
            html = await reader.ReadToEndAsync();
        }
        else if (extension == ".html")
        {
            buffered.Position = 0;
            using var reader = new StreamReader(buffered, Encoding.UTF8, true, 1024, true);
            html = await reader.ReadToEndAsync();
        }
        else
            throw new PluginMisconfigurationException("The XLIFF file has no usable target content. Preserve the original HTML skeleton and complete the translation first.");
        if (loaded.Success && loaded.WasBilingual && !string.IsNullOrWhiteSpace(loaded.Value.TargetLanguage) && loaded.Value.TargetLanguage != input.Locale)
            throw new PluginMisconfigurationException("Target language does not match the language in the XLIFF file. Select its exact target language.");
        var (document, manifest) = ContentRoundtrip.Parse(html);
        if (manifest.SpaceId != Client.SpaceId)
            throw new PluginMisconfigurationException("This file belongs to another Builder Space. Select its original connection.");
        if (input.Locale == manifest.Locale)
            throw new PluginMisconfigurationException("Target language must differ from the source language for a translation upload.");
        var actions = new ContentActions(InvocationContext);
        var model = await actions.ResolveModel(manifest.ModelId);
        var source = await actions.Read(model, manifest.EntryId);
        var latest = ContentRoundtrip.Extract(source, model, Client.SpaceId, manifest.Locale, manifest.Mode);
        if (JsonConvert.SerializeObject(latest.Fields) != JsonConvert.SerializeObject(manifest.Fields))
            throw new PluginMisconfigurationException("The source fields or model changed, or the file metadata was modified. Download and translate the current content.");
        var targetId = manifest.Mode == "field" ? source.Id : input.ContentId ?? ContentRoundtrip.TargetId(Client.SpaceId, model.Id, manifest.RootId, input.Locale);
        if (manifest.Mode == "field" && input.ContentId != null && input.ContentId != source.Id)
            throw new PluginMisconfigurationException("Field-level localization writes to the original entry. Leave Target content ID empty or use the source ID.");
        if (manifest.Mode == "entry" && targetId == source.Id)
            throw new PluginMisconfigurationException("Whole-entry localization needs a separate target entry. Do not select the source entry.");
        var existing = manifest.Mode == "field" ? source : await actions.Find(model, targetId);
        if (input.ContentId != null && existing == null)
            throw new PluginMisconfigurationException("The supplied target entry was not found. Check its ID or leave Target content ID empty to create a translation.");
        if (existing != null && existing.Published != "draft")
            throw new PluginMisconfigurationException("The target entry is not a draft. Explicitly unpublish it before uploading, then publish it after review. Inline localization affects the entire entry.");
        if (manifest.Mode == "entry" && existing != null)
        {
            var linked = existing.Meta["blackbirdSourceId"]?.Value<string>();
            var language = existing.Meta["blackbirdTargetLocale"]?.Value<string>();
            if ((linked != null && linked != manifest.RootId) || (language != null && language != input.Locale))
                throw new PluginMisconfigurationException("This target entry is linked to a different source or locale. Select the correct translation entry.");
            if (linked == null && input.ContentId == null)
                throw new PluginMisconfigurationException("The generated target ID is already in use. Supply a separate target entry explicitly.");
        }
        var target = existing ?? new ContentDto
        {
            Id = targetId,
            Name = $"{source.Name} ({input.Locale})",
            Data = (JObject)source.Data.DeepClone(),
            Meta = (JObject)source.Meta.DeepClone(),
            Query = (JArray)source.Query.DeepClone(),
            Published = "draft"
        };
        var updated = ContentRoundtrip.Apply(document, manifest, source, target, input.Locale);
        var meta = (JObject)target.Meta.DeepClone();
        var query = (JArray)target.Query.DeepClone();
        if (manifest.Mode == "entry")
        {
            meta["blackbirdSourceId"] = manifest.RootId;
            meta["blackbirdTargetLocale"] = input.Locale;
            query = new JArray(query.Where(q => q["property"]?.Value<string>() != "locale"));
            query.Add(new JObject { ["property"] = "locale", ["operator"] = "is", ["value"] = input.Locale });
        }
        var changed = existing == null || !JToken.DeepEquals(target.Data, updated) || !JToken.DeepEquals(target.Meta, meta) || !JToken.DeepEquals(target.Query, query);
        if (changed)
        {
            if (existing != null)
            {
                var check = await actions.Read(model, target.Id);
                if (check.LastUpdated != existing.LastUpdated)
                    throw new PluginMisconfigurationException("Target content changed during upload. Retry after reviewing the latest draft.");
            }
            var body = existing == null
                ? new Dictionary<string, object> { ["name"] = target.Name, ["published"] = "draft", ["data"] = updated, ["meta"] = meta, ["query"] = query }
                : new Dictionary<string, object> { ["data"] = updated, ["meta"] = meta, ["query"] = query };
            await Client.ExecuteWithErrorHandling(Client.WriteRequest(model.Name, target.Id, existing == null ? Method.Put : Method.Patch)
                .AddQueryParameter("triggerWebhooks", "false")
                .AddStringBody(JsonConvert.SerializeObject(body), DataFormat.Json));
        }
        var result = new ContentFileResponse { RootEntryId = manifest.RootId, TargetEntryId = target.Id, ModelId = model.Id, Changed = changed };
        if (loaded.Success)
        {
            var transformation = loaded.Value;
            transformation.TargetSystemReference = Reference(manifest, target);
            transformation.TargetLanguage = input.Locale;
            if (loaded.WasBilingual)
            {
                using var output = transformation.ToStream();
                result.Content = await files.UploadAsync(output, "application/xliff+xml", transformation.BilingualFileName);
                return result;
            }
        }
        ContentRoundtrip.Stamp(document, manifest, target.Id, target.Name, input.Locale);
        var coded = new HtmlCoder().Deserialize(document.DocumentNode.OuterHtml, ContentRoundtrip.FileName(target.Name, input.Locale));
        coded.SystemReference = Reference(manifest, target);
        coded.Language = input.Locale;
        using var contentStream = coded.ToStream();
        result.Content = await files.UploadAsync(contentStream, "text/html", ContentRoundtrip.FileName(target.Name, input.Locale));
        return result;
    }

    private static SystemReference Reference(RoundtripManifest manifest, ContentDto entry)
    {
        return new SystemReference
        {
            ContentId = $"{manifest.SpaceId}:{manifest.ModelId}:{manifest.RootId}",
            ContentName = entry.Name,
            AdminUrl = $"https://builder.io/content/{Uri.EscapeDataString(entry.Id)}",
            SystemName = "Builder.io",
            SystemRef = "https://www.builder.io"
        };
    }
}
