using Apps.BuilderIo.Actions;
using Apps.BuilderIo.Models.Identifiers;
using Apps.BuilderIo.Models.Requests;
using Tests.BuilderIo.Base;

namespace Tests.BuilderIo;

[TestClass]
public class TagActionsTests : TestBase
{
    [TestMethod]
    public async Task TagActions_PreserveExistingTagsAndAvoidDuplicates()
    {
        await using var fixture = new Fixture(Client);
        await fixture.Initialize();
        var actions = new TagActions(Context);
        var input = new ChangeTagsRequest { ModelId = fixture.ModelId, ContentId = fixture.ContentId, TagField = "tag", Tags = ["reviewed", "reviewed"] };
        var added = await actions.AddContentTags(input);
        CollectionAssert.AreEquivalent(new[] { "keep", "localize", "reviewed" }, added.Tags.ToArray());
        var fetched = await actions.GetContentTags(input);
        CollectionAssert.AreEquivalent(added.Tags.ToArray(), fetched.Tags.ToArray());
        var all = await actions.SearchTags(new TagFieldIdentifier { ModelId = fixture.ModelId, TagField = "tag" });
        CollectionAssert.Contains(all.Tags.ToList(), "reviewed");
        var removed = await actions.RemoveContentTags(input);
        CollectionAssert.AreEquivalent(new[] { "keep", "localize" }, removed.Tags.ToArray());
        var model = await new ContentActions(Context).ResolveModel(fixture.ModelId);
        var entry = await new ContentActions(Context).Read(model, fixture.ContentId);
        Assert.AreEqual("Hello world", entry.Data["title"]?["Default"]?.ToString());
    }
}
