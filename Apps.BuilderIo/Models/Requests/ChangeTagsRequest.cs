using Apps.BuilderIo.Handlers;
using Apps.BuilderIo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.BuilderIo.Models.Requests;

public class ChangeTagsRequest : ContentTagsRequest
{
    [Display("Tags", Description = "Select suggestions or enter new tag text.")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string> Tags { get; set; } = [];


}
