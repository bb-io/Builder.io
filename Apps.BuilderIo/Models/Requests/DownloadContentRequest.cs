using Apps.BuilderIo.Handlers;
using Apps.BuilderIo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.BuilderIo.Models.Requests;

public class DownloadContentRequest : Apps.BuilderIo.Models.Identifiers.ContentIdentifier, IDownloadContentInput
{
    [Display("Source language", Description = "Select the source language explicitly. Default reads the Builder default value.")]
    [DataSource(typeof(LocaleDataHandler))]
    public string Locale { get; set; } = "";

    [Display("Localization mode (default: Field-level)", Description = "When omitted, uses Field-level: translates explicitly localized fields within the same entry. Select Whole-entry localization to translate supported plain text into a separate locale-targeted entry.")]
    [StaticDataSource(typeof(ModeDataHandler))]
    public string? Mode { get; set; }


}
