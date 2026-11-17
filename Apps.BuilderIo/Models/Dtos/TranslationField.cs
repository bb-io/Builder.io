namespace Apps.BuilderIo.Models.Dtos;

public class TranslationField
{
    public List<string> Path { get; set; } = [];

    public string Key { get; set; } = "";

    public string Value { get; set; } = "";

    public bool Html { get; set; }

    public int? MaximumLength { get; set; }
}
