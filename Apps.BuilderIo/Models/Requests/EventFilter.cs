using Apps.BuilderIo.Handlers;
using Apps.BuilderIo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.BuilderIo.Models.Requests;

public class EventFilter
{
    [Display("Content ID")]
    [DataSource(typeof(ContentDataHandler))]
    public string? ContentId { get; set; }

    [Display("Tag field")]
    [DataSource(typeof(TagFieldDataHandler))]
    public string? TagField { get; set; }

    [Display("Tags")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string>? Tags { get; set; }


}
