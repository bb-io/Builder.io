using Apps.BuilderIo.Handlers;
using Apps.BuilderIo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.BuilderIo.Models.Responses;

public class ContentFileResponse : IDownloadContentOutput
{
    [Display("Content")]
    public FileReference Content { get; set; } = default!;

    [Display("Source content ID")]
    public string RootEntryId { get; set; } = "";

    [Display("Target content ID")]
    public string TargetEntryId { get; set; } = "";

    [Display("Model ID")]
    public string ModelId { get; set; } = "";

    [Display("Content changed")]
    public bool Changed { get; set; }


}
