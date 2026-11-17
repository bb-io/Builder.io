using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.BuilderIo.Utils;

public static class Input
{
    public static string Required(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new PluginMisconfigurationException($"Please fill in '{name}'.");
        return value.Trim();
    }
}
