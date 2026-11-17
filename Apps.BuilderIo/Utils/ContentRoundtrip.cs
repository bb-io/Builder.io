using System.Net;
using System.Security.Cryptography;
using System.Text;
using Apps.BuilderIo.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Exceptions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.BuilderIo.Utils;

public static class ContentRoundtrip
{
    private const string LocalizedType = "@builder.io/core:LocalizedValue";
    private static readonly HashSet<string> TextTypes = ["string", "text", "longText", "richText", "html", "markdown"];
    private static readonly HashSet<string> BuiltinText = ["text", "label", "altText", "title", "placeholder"];
    private static readonly HashSet<string> Excluded = ["@type", "id", "url", "href", "src", "image", "video", "file", "code", "jsCode", "cssCode", "bindings", "actions", "responsiveStyles", "style", "state", "meta", "trackingPixel", "tagName"];

    public static RoundtripManifest Extract(ContentDto entry, ModelDto model, string space, string locale, string mode)
    {
        if (mode is not ("field" or "entry"))
            throw new PluginMisconfigurationException("Choose Field-level or Whole-entry localization.");
        var data = Normalize(entry.Data);
        var root = entry.Meta["blackbirdSourceId"]?.Value<string>() ?? entry.Id;
        var result = new RoundtripManifest
        {
            SpaceId = space,
            ModelId = model.Id,
            EntryId = entry.Id,
            RootId = root,
            Locale = locale,
            Mode = mode,
            SourceData = data,
            SourceHash = Hash(Project(data, locale).ToString(Formatting.None))
        };
        foreach (var field in model.Fields.Where(f => f.Type != "tags" && !Excluded.Contains(f.Name)))
            Schema(data[field.Name], field, [field.Name], result, false);
        if (data["blocks"] is JArray blocks)
            Blocks(blocks, ["blocks"], result);
        if (result.Fields.Count == 0)
            throw new PluginMisconfigurationException("No supported translatable text was found. For field-level mode, enable localization on text fields or component inputs. References and Symbols must be downloaded separately.");
        if (result.Fields.Select(x => x.Key).Distinct().Count() != result.Fields.Count)
            throw new PluginMisconfigurationException("Duplicate field identifiers were found. Check component IDs and model fields before exporting.");
        return result;
    }

    public static string Export(RoundtripManifest manifest, string title)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body></body></html>");
        doc.DocumentNode.SelectSingleNode("//html").SetAttributeValue("lang", WebUtility.HtmlEncode(manifest.Locale));
        Stamp(doc, manifest, manifest.EntryId, title, manifest.Locale);
        var body = doc.DocumentNode.SelectSingleNode("//body");
        body.SetAttributeValue("its-rev-tool", "Builder.io");
        body.SetAttributeValue("its-rev-tool-ref", "https://www.builder.io");
        var container = doc.CreateElement("div");
        container.SetAttributeValue("data-builder-entry-id", WebUtility.HtmlEncode(manifest.EntryId));
        container.SetAttributeValue("data-builder-manifest", Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(manifest))));
        body.AppendChild(container);
        foreach (var field in manifest.Fields)
        {
            var node = doc.CreateElement("div");
            node.SetAttributeValue("data-builder-field-id", field.Key);
            node.SetAttributeValue("data-builder-field-type", field.Html ? "html" : "text");
            node.SetAttributeValue("data-blackbird-key", field.Key);
            if (field.MaximumLength.HasValue)
                node.SetAttributeValue("data-blackbird-size", WebUtility.HtmlEncode(JsonConvert.SerializeObject(new { MaximumSize = field.MaximumLength.Value })));
            node.InnerHtml = field.Html ? field.Value : WebUtility.HtmlEncode(field.Value);
            container.AppendChild(node);
        }
        return doc.DocumentNode.OuterHtml;
    }

    public static (HtmlDocument Document, RoundtripManifest Manifest) Parse(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var nodes = doc.DocumentNode.SelectNodes("//*[@data-builder-manifest]");
            if (nodes == null || nodes.Count != 1)
                throw new PluginMisconfigurationException("Use a complete file produced by Download content. Its round-trip metadata is missing or duplicated.");
            var encoded = nodes[0].GetAttributeValue("data-builder-manifest", "");
            var manifest = JsonConvert.DeserializeObject<RoundtripManifest>(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)))
                ?? throw new FormatException();
            if (manifest.Version != 1 || manifest.Fields.Count == 0 || manifest.Mode is not ("field" or "entry"))
                throw new FormatException();
            return (doc, manifest);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            throw new PluginMisconfigurationException("The translation file metadata is invalid or unsupported. Download the content again and preserve file metadata during translation.");
        }
    }

    public static JObject Apply(HtmlDocument doc, RoundtripManifest manifest, ContentDto currentSource, ContentDto target, string targetLocale)
    {
        var source = Normalize(currentSource.Data);
        if (Hash(Project(source, manifest.Locale).ToString(Formatting.None)) != manifest.SourceHash)
            throw new PluginMisconfigurationException("Source content changed after download. Download and translate the latest version before uploading.");
        var result = Normalize(target.Data);
        var nodes = doc.DocumentNode.SelectNodes("//*[@data-builder-field-id]")?.ToList() ?? [];
        if (nodes.Count != manifest.Fields.Count || nodes.Select(n => n.GetAttributeValue("data-builder-field-id", "")).Distinct().Count() != nodes.Count)
            throw new PluginMisconfigurationException("Translation fields are missing or duplicated. Use the complete translated file without removing field metadata.");
        foreach (var field in manifest.Fields)
        {
            var node = nodes.SingleOrDefault(n => n.GetAttributeValue("data-builder-field-id", "") == field.Key)
                ?? throw new PluginMisconfigurationException("A translation field could not be matched. Download the content again.");
            var text = field.Html ? node.InnerHtml : HtmlEntity.DeEntitize(node.InnerText);
            var original = field.Html ? NormalizeHtml(field.Value) : field.Value;
            if (text == original)
                text = field.Value;
            if (field.MaximumLength.HasValue && (field.Html ? HtmlEntity.DeEntitize(node.InnerText).Length : text.Length) > field.MaximumLength.Value)
                throw new PluginMisconfigurationException($"A translated field exceeds its {field.MaximumLength.Value}-character limit. Shorten the translation and retry.");
            Set(result, source, field.Path, targetLocale, manifest.Locale, manifest.Mode, text);
        }
        if (target.Data["blocksString"] != null && target.Data["blocks"] == null)
        {
            result["blocksString"] = result["blocks"]?.ToString(Formatting.None);
            result.Remove("blocks");
        }
        return result;
    }

    public static void Stamp(HtmlDocument doc, RoundtripManifest manifest, string entryId, string name, string locale)
    {
        doc.DocumentNode.SelectSingleNode("//html")?.SetAttributeValue("lang", WebUtility.HtmlEncode(locale));
        var head = doc.DocumentNode.SelectSingleNode("//head");
        if (head == null)
            throw new PluginMisconfigurationException("The content file has no document header.");
        var values = new Dictionary<string, string>
        {
            ["blackbird-ucid"] = $"{manifest.SpaceId}:{manifest.ModelId}:{manifest.RootId}",
            ["blackbird-content-name"] = name,
            ["blackbird-admin-url"] = $"https://builder.io/content/{Uri.EscapeDataString(entryId)}",
            ["blackbird-system-name"] = "Builder.io",
            ["blackbird-system-ref"] = "https://www.builder.io"
        };
        foreach (var pair in values)
        {
            var node = head.SelectSingleNode($"meta[@name='{pair.Key}']") ?? doc.CreateElement("meta");
            node.SetAttributeValue("name", pair.Key);
            node.SetAttributeValue("content", WebUtility.HtmlEncode(pair.Value));
            if (node.ParentNode == null)
                head.AppendChild(node);
        }
    }

    public static string TargetId(string space, string model, string source, string locale)
        => Hash($"{space}|{model}|{source}|{locale}")[..32].ToLowerInvariant();

    public static string FileName(string name, string locale)
    {
        var safe = new string(name.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').Take(100).ToArray());
        var language = new string(locale.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return $"{(safe.Length == 0 ? "content" : safe)}_{language}.html";
    }

    private static void Schema(JToken? value, FieldDto field, List<string> path, RoundtripManifest result, bool insideLocalized)
    {
        if (value == null || value.Type == JTokenType.Null || Excluded.Contains(field.Name) || field.Type is "tags" or "reference" or "file" or "url")
            return;
        var localized = IsLocalized(value);
        if (localized)
        {
            var selected = value[result.Locale] ?? value["Default"];
            var copy = new FieldDto { Name = field.Name, Type = field.Type, SubFields = field.SubFields, Options = field.Options };
            Schema(selected, copy, [.. path, "$locale"], result, true);
            return;
        }
        if (value.Type == JTokenType.String && TextTypes.Contains(field.Type))
        {
            if (result.Mode == "entry" || insideLocalized || field.Localized)
            {
                var dest = !insideLocalized && field.Localized && result.Mode == "field" ? new List<string>([.. path, "$locale"]) : path;
                Add(value.Value<string>()!, dest, result, field.Type is "richText" or "html", field.Options["maxLength"]?.Value<int?>());
            }
            return;
        }
        if (value is JArray array && field.Type == "list")
        {
            if (result.Mode == "entry" && array.Any(x => x is not JObject || string.IsNullOrWhiteSpace(x["id"]?.Value<string>())))
                throw new PluginMisconfigurationException($"Whole-entry list '{field.Name}' needs stable item IDs to preserve target edits safely. Add an id field to each item or use field-level localization.");
            for (var i = 0; i < array.Count; i++)
                foreach (var sub in field.SubFields)
                    Schema(array[i][sub.Name], sub, [.. path, Index(array[i], i), sub.Name], result, insideLocalized);
        }
        else if (value is JObject obj && field.Type == "object")
        {
            foreach (var sub in field.SubFields)
                Schema(obj[sub.Name], sub, [.. path, sub.Name], result, insideLocalized);
        }
        else if (insideLocalized && value is JContainer)
            throw new PluginMisconfigurationException($"Localized field '{field.Name}' needs a supported text, object or list schema. Configure its subfields before exporting.");
    }

    private static void Blocks(JToken token, List<string> path, RoundtripManifest result)
    {
        if (token is JArray array)
        {
            for (var i = 0; i < array.Count; i++)
                Blocks(array[i], [.. path, Index(array[i], i)], result);
            return;
        }
        if (token is not JObject block || block["meta"]?["excludeFromTranslation"]?.Value<bool>() == true)
            return;
        var component = block["component"]?["name"]?.Value<string>();
        if (component == "Symbol")
            return;
        var options = block["component"]?["options"] as JObject;
        if (options != null)
        {
            var excluded = block["meta"]?["nonTranslatableInputs"]?.Values<string>().ToHashSet() ?? [];
            foreach (var prop in options.Properties())
            {
                if (excluded.Contains(prop.Name) || Excluded.Contains(prop.Name))
                    continue;
                var allowPlain = result.Mode == "entry" && component is "Text" or "Button" or "Image" or "TextArea" or "Input" && BuiltinText.Contains(prop.Name);
                WalkLocalized(prop.Value, [.. path, "component", "options", prop.Name], result, allowPlain, prop.Name == "text" && component == "Text");
            }
        }
        if (block["children"] is JArray children)
            Blocks(children, [.. path, "children"], result);
        if (options != null)
        {
            foreach (var prop in options.Properties().Where(p => p.Value is JArray))
            {
                var items = (JArray)prop.Value;
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i] is JObject item)
                        foreach (var nested in item.Properties().Where(p => p.Value is JArray && p.Name == "blocks"))
                            Blocks(nested.Value, [.. path, "component", "options", prop.Name, Index(items[i], i), nested.Name], result);
                }
            }
        }
    }

    private static void WalkLocalized(JToken value, List<string> path, RoundtripManifest result, bool plain, bool html)
    {
        if (IsLocalized(value))
        {
            var selected = value[result.Locale] ?? value["Default"];
            if (selected?.Type == JTokenType.String)
                Add(selected.Value<string>()!, [.. path, "$locale"], result, html || LooksHtml(selected.Value<string>()!), null);
            else if (selected is JContainer)
                throw new PluginMisconfigurationException("A localized component input contains an object or list. This release supports text component inputs; move nested text into individually localized inputs before exporting.");
            return;
        }
        if (value.Type == JTokenType.String && plain)
            Add(value.Value<string>()!, path, result, html, null);
        if (value is JObject obj)
            foreach (var prop in obj.Properties().Where(p => !Excluded.Contains(p.Name)))
                WalkLocalized(prop.Value, [.. path, prop.Name], result, false, false);
        if (value is JArray array)
            for (var i = 0; i < array.Count; i++)
                WalkLocalized(array[i], [.. path, Index(array[i], i)], result, false, false);
    }

    private static void Add(string value, List<string> path, RoundtripManifest result, bool html, int? maximum)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        result.Fields.Add(new TranslationField
        {
            Path = path,
            Key = Hash($"{result.SpaceId}:{result.ModelId}:{result.RootId}:" + JsonConvert.SerializeObject(path)).ToLowerInvariant(),
            Value = value,
            Html = html,
            MaximumLength = maximum
        });
    }

    private static void Set(JToken target, JToken source, List<string> path, string locale, string sourceLocale, string mode, string text)
    {
        for (var i = 0; i < path.Count; i++)
        {
            var segment = path[i];
            var last = i == path.Count - 1;
            if (segment == "$locale")
            {
                if (!IsLocalized(target))
                {
                    if (target.Parent == null)
                        throw new PluginMisconfigurationException("Cannot restore a localized field. Download the content again.");
                    var wrapper = new JObject { ["@type"] = LocalizedType, ["Default"] = target.DeepClone() };
                    target.Replace(wrapper);
                    target = wrapper;
                }
                var src = IsLocalized(source) ? source[sourceLocale] ?? source["Default"] : source;
                var key = locale;
                if (last)
                {
                    target[key] = text;
                    return;
                }
                target[key] ??= src?.DeepClone();
                source = src ?? throw new PluginMisconfigurationException("A source localized field has no value.");
                target = target[key]!;
                continue;
            }
            var srcChild = Child(source, segment);
            var tgtChild = Child(target, segment);
            if (srcChild == null || tgtChild == null)
                throw new PluginMisconfigurationException("A translated field no longer exists in the target layout. Restore its field or block, or create a new translation entry.");
            if (last)
            {
                tgtChild.Replace(new JValue(text));
                return;
            }
            source = srcChild;
            target = tgtChild;
        }
    }

    private static JToken? Child(JToken value, string segment)
    {
        if (value is JObject obj)
            return obj[segment];
        if (value is not JArray array)
            return null;
        if (segment.StartsWith("@"))
        {
            var matches = array.Where(x => x is JObject && x["id"]?.Value<string>() == segment[1..]).Take(2).ToList();
            if (matches.Count > 1)
                throw new PluginMisconfigurationException("The target contains duplicate block or list item IDs. Make the IDs unique before uploading.");
            return matches.SingleOrDefault();
        }
        return int.TryParse(segment.TrimStart('#'), out var i) && i >= 0 && i < array.Count ? array[i] : null;
    }

    private static string Index(JToken token, int index)
        => token is JObject obj && !string.IsNullOrWhiteSpace(obj["id"]?.Value<string>()) ? "@" + obj["id"]!.Value<string>() : "#" + index;

    private static JObject Normalize(JObject data)
    {
        var clone = (JObject)data.DeepClone();
        if (clone["blocks"] == null && clone["blocksString"]?.Type == JTokenType.String)
        {
            try
            {
                clone["blocks"] = JArray.Parse(clone["blocksString"]!.Value<string>()!);
                clone.Remove("blocksString");
            }
            catch (JsonException)
            {
                throw new PluginMisconfigurationException("The entry contains invalid serialized blocks. Repair it in Builder.io before downloading.");
            }
        }
        return clone;
    }

    private static JToken Project(JToken value, string locale)
    {
        if (IsLocalized(value))
            return Project(value[locale] ?? value["Default"] ?? JValue.CreateNull(), locale);
        if (value is JObject obj)
            return new JObject(obj.Properties().OrderBy(x => x.Name, StringComparer.Ordinal).Select(p => new JProperty(p.Name, Project(p.Value, locale))));
        if (value is JArray array)
            return new JArray(array.Select(x => Project(x, locale)));
        return value.DeepClone();
    }

    private static bool IsLocalized(JToken value) => value is JObject && value["@type"]?.Value<string>() == LocalizedType;

    private static bool LooksHtml(string value) => value.Contains('<') && value.Contains('>');

    private static string NormalizeHtml(string value)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(value);
        return doc.DocumentNode.InnerHtml;
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
