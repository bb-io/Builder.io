using Apps.BuilderIo.Handlers;
using Apps.BuilderIo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.BuilderIo.Models.Responses;

public class ContentResponse : IDownloadContentInput
{
    [Display("Content ID")]
    public string ContentId { get; set; } = "";

    [Display("Model ID")]
    public string ModelId { get; set; } = "";

    [Display("Name")]
    public string Name { get; set; } = "";

    [Display("Status")]
    public string Status { get; set; } = "";

    [Display("Updated at")]
    public DateTime UpdatedAt { get; set; }

    [Display("Editor URL")]
    public string AdminUrl { get; set; } = "";

    [Display("Tags", Description = "Combined values from non-localized tags fields on the model.")]
    public IEnumerable<string> Tags { get; set; } = [];


}
