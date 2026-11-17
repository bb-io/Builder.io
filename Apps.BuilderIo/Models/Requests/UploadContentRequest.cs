using Apps.BuilderIo.Handlers;
using Apps.BuilderIo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.BuilderIo.Models.Requests;

public class UploadContentRequest : IUploadContentInput
{
    [Display("Translation file")]
    public FileReference Content { get; set; } = default!;

    [Display("Target language")]
    [DataSource(typeof(LocaleDataHandler))]
    public string Locale { get; set; } = "";

    [Display("Target content ID", Description = "Optional manual or mapped ID of an existing whole-entry translation. The source model comes from the file.")]
    public string? ContentId { get; set; }


}
