using Newtonsoft.Json.Linq;

namespace Apps.BuilderIo.Models.Dtos;

public class RoundtripManifest
{
    public int Version { get; set; } = 1;

    public string SpaceId { get; set; } = "";

    public string ModelId { get; set; } = "";

    public string EntryId { get; set; } = "";

    public string RootId { get; set; } = "";

    public string Locale { get; set; } = "";

    public string Mode { get; set; } = "field";

    public string SourceHash { get; set; } = "";

    public JObject SourceData { get; set; } = new();

    public List<TranslationField> Fields { get; set; } = [];
}
