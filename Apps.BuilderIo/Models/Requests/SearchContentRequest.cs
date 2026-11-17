using Apps.BuilderIo.Handlers;
using Apps.BuilderIo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.BuilderIo.Models.Requests;

public class SearchContentRequest : Apps.BuilderIo.Models.Identifiers.ModelIdentifier
{
    [Display("Name contains")]
    public string? NameContains { get; set; }

    [Display("Status")]
    [StaticDataSource(typeof(StatusDataHandler))]
    public string? Status { get; set; }

    [Display("Updated after")]
    public DateTime? UpdatedAfter { get; set; }

    [Display("Tag field")]
    [DataSource(typeof(TagFieldDataHandler))]
    public string? TagField { get; set; }

    [Display("Tags", Description = "Matches entries containing all supplied tags. Select a tag field first.")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string>? Tags { get; set; }


}
