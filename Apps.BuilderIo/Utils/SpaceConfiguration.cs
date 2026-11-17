using Apps.BuilderIo.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;

namespace Apps.BuilderIo.Utils;

public static class SpaceConfiguration
{
    public static List<string> Locales(SpaceDto space)
    {
        var locales = space.Settings["customTargetingAttributes"]?["locale"]?["enum"]?.Values<string>()
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToList() ?? [];
        return locales.Prepend("Default").Distinct(StringComparer.Ordinal).ToList();
    }

    public static void ValidateLocale(SpaceDto space, string locale)
    {
        Input.Required(locale, "Language");
        if (!Locales(space).Contains(locale, StringComparer.Ordinal))
            throw new PluginMisconfigurationException("This language is not configured in the connected Space. Add it in Builder.io localization settings first.");
    }

    public static FieldDto TagField(ModelDto model, string name)
    {
        Input.Required(name, "Tag field");
        return model.Fields.FirstOrDefault(f => f.Name == name && f.Type == "tags" && !f.Localized)
            ?? throw new PluginMisconfigurationException("Select a non-localized tags field on this model. Add one in Builder.io Models if none exists.");
    }

    public static List<string> ReadTags(ContentDto content, string field)
    {
        var value = content.Data[field];
        if (value == null || value.Type == JTokenType.Null)
            return [];
        if (value is not JArray array || array.Any(x => x.Type != JTokenType.String))
            throw new PluginMisconfigurationException("The selected tags field does not contain a list of tag text. Check its model configuration.");
        return array.Values<string>().Select(x => x!).Distinct(StringComparer.Ordinal).ToList();
    }
}
