using Apps.BuilderIo.Handlers;
using Apps.BuilderIo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.BuilderIo.Models.Identifiers;

public class ContentIdentifier : ModelIdentifier
{
    [Display("Content ID")]
    [DataSource(typeof(ContentDataHandler))]
    public string ContentId { get; set; } = "";


}
