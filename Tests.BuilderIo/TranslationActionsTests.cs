using System.Text;
using Apps.BuilderIo.Actions;
using Apps.BuilderIo.Models.Requests;
using Apps.BuilderIo.Utils;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using HtmlAgilityPack;
using Blackbird.Filters.Coders;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Tests.BuilderIo.Base;

namespace Tests.BuilderIo;

[TestClass]
public class TranslationActionsTests : TestBase
{
    [TestMethod]
    public async Task FieldRoundtrip_PreservesSourceTagsAndOtherData()
    {
        await using var fixture = new Fixture(Client);
        await fixture.Initialize();
        var (sourceLocale, targetLocale) = await Languages();
        var actions = new TranslationActions(Context, Files);
        var file = await actions.DownloadContent(new DownloadContentRequest { ModelId = fixture.ModelId, ContentId = fixture.ContentId, Locale = sourceLocale });
        var translated = await Translate(file.Content);
        var output = await actions.UploadContent(new UploadContentRequest { Content = translated, Locale = targetLocale });
        Assert.AreEqual(fixture.ContentId, output.TargetEntryId);
        var content = new ContentActions(Context);
        var model = await content.ResolveModel(fixture.ModelId);
        var entry = await content.Read(model, fixture.ContentId);
        Assert.AreEqual("Hello world", entry.Data["title"]?["Default"]?.ToString());
        Assert.AreEqual("Translated title", entry.Data["title"]?[targetLocale]?.ToString());
        Assert.AreEqual("https://example.com/source", entry.Data["slug"]?.ToString());
        CollectionAssert.AreEquivalent(new[] { "keep", "localize" }, entry.Data["tag"]!.Values<string>().ToArray());
        var repeated = await actions.UploadContent(new UploadContentRequest { Content = translated, Locale = targetLocale });
        Assert.IsFalse(repeated.Changed);
        using var outputStream = await Files.DownloadAsync(output.Content);
        using var reader = new StreamReader(outputStream);
        var outputHtml = await reader.ReadToEndAsync();
        StringAssert.Contains(outputHtml, "blackbird-ucid");
        StringAssert.Contains(outputHtml, targetLocale);
    }

    [TestMethod]
    public async Task WholeEntryRoundtrip_ReusesTargetAndPreservesTargetEdits()
    {
        await using var fixture = new Fixture(Client);
        await fixture.Initialize();
        var (sourceLocale, targetLocale) = await Languages();
        var actions = new TranslationActions(Context, Files);
        var file = await actions.DownloadContent(new DownloadContentRequest { ModelId = fixture.ModelId, ContentId = fixture.ContentId, Locale = sourceLocale, Mode = "entry" });
        var translated = await Translate(file.Content);
        var first = await actions.UploadContent(new UploadContentRequest { Content = translated, Locale = targetLocale });
        Assert.AreNotEqual(fixture.ContentId, first.TargetEntryId);
        var content = new ContentActions(Context);
        var model = await content.ResolveModel(fixture.ModelId);
        var entry = await content.Read(model, first.TargetEntryId);
        entry.Data["slug"] = "https://example.com/market";
        await Client.ExecuteWithErrorHandling(Client.WriteRequest(model.Name, entry.Id, Method.Patch)
            .AddStringBody(JsonConvert.SerializeObject(new { data = entry.Data }), DataFormat.Json));
        var repeated = await actions.UploadContent(new UploadContentRequest { Content = translated, Locale = targetLocale });
        Assert.AreEqual(first.TargetEntryId, repeated.TargetEntryId);
        var updated = await content.Read(model, first.TargetEntryId);
        Assert.AreEqual("https://example.com/market", updated.Data["slug"]?.ToString());
        Assert.AreEqual(fixture.ContentId, updated.Meta["blackbirdSourceId"]?.ToString());
        Assert.IsTrue(updated.Query.Any(q => q["property"]?.ToString() == "locale" && q["value"]?.ToString() == targetLocale));
    }

    [TestMethod]
    public async Task ChangedSource_BlocksStaleUpload()
    {
        await using var fixture = new Fixture(Client);
        await fixture.Initialize();
        var (sourceLocale, targetLocale) = await Languages();
        var actions = new TranslationActions(Context, Files);
        var file = await actions.DownloadContent(new DownloadContentRequest { ModelId = fixture.ModelId, ContentId = fixture.ContentId, Locale = sourceLocale });
        var content = new ContentActions(Context);
        var model = await content.ResolveModel(fixture.ModelId);
        var entry = await content.Read(model, fixture.ContentId);
        entry.Data["title"]!["Default"] = "New source";
        await Client.ExecuteWithErrorHandling(Client.WriteRequest(model.Name, entry.Id, Method.Patch).AddStringBody(JsonConvert.SerializeObject(new { data = entry.Data }), DataFormat.Json));
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() => actions.UploadContent(new UploadContentRequest { Content = file.Content, Locale = targetLocale }));
    }

    [TestMethod]
    public async Task XliffRoundtrip_UpdatesTargetAndReturnsBilingualMetadata()
    {
        await using var fixture = new Fixture(Client);
        await fixture.Initialize();
        var (sourceLocale, targetLocale) = await Languages();
        var actions = new TranslationActions(Context, Files);
        var file = await actions.DownloadContent(new DownloadContentRequest { ModelId = fixture.ModelId, ContentId = fixture.ContentId, Locale = sourceLocale });
        using var original = await Files.DownloadAsync(file.Content);
        using var reader = new StreamReader(original);
        var coded = new HtmlCoder().Deserialize(await reader.ReadToEndAsync(), file.Content.Name);
        var transformation = coded.CreateTransformation(targetLocale);
        foreach (var segment in transformation.GetUnits().SelectMany(u => u.Segments))
        {
            segment.SetTarget(segment.GetSource().Replace("Hello", "Translated"));
            segment.State = SegmentState.Translated;
        }
        using var bilingual = transformation.ToStream();
        var translated = await Files.UploadAsync(bilingual, "application/xliff+xml", "translated.xlf");
        var result = await actions.UploadContent(new UploadContentRequest { Content = translated, Locale = targetLocale });
        using var output = await Files.DownloadAsync(result.Content);
        var loaded = Transformation.Load(output, result.Content.Name, result.Content.ContentType);
        Assert.IsTrue(loaded.Success);
        Assert.IsTrue(loaded.WasBilingual);
        Assert.AreEqual(targetLocale, loaded.Value.TargetLanguage);
        StringAssert.Contains(loaded.Value.TargetSystemReference.ContentId, fixture.ContentId);
        var content = new ContentActions(Context);
        var model = await content.ResolveModel(fixture.ModelId);
        var entry = await content.Read(model, fixture.ContentId);
        StringAssert.Contains(entry.Data["title"]![targetLocale]!.ToString(), "Translated");
    }

    private async Task<(string Source, string Target)> Languages()
    {
        var locales = (await new ContentActions(Context).SearchLocales()).Locales.Where(l => l != "Default").ToList();
        Assert.IsTrue(locales.Count >= 2, "Configure at least two locales in the disposable test Space before running localization tests.");
        return (locales[0], locales[1]);
    }

    private async Task<FileReference> Translate(FileReference reference)
    {
        using var stream = await Files.DownloadAsync(reference);
        using var reader = new StreamReader(stream);
        var html = await reader.ReadToEndAsync();
        var (doc, manifest) = ContentRoundtrip.Parse(html);
        var title = manifest.Fields.Single(f => f.Path[0] == "title");
        doc.DocumentNode.SelectSingleNode($"//*[@data-builder-field-id='{title.Key}']").InnerHtml = "Translated title";
        using var output = new MemoryStream(Encoding.UTF8.GetBytes(doc.DocumentNode.OuterHtml));
        return await Files.UploadAsync(output, "text/html", "translated.html");
    }
}
