using Apps.BuilderIo.Handlers;
using Apps.BuilderIo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.BuilderIo.Models.Responses;

public class ModelResponse
{
    [Display("Model ID")]
    public string ModelId { get; set; } = "";

    [Display("Name")]
    public string Name { get; set; } = "";

    [Display("Model type")]
    public string Kind { get; set; } = "";


}
