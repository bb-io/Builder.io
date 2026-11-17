using Apps.BuilderIo.Api;
using Apps.BuilderIo.Models.Dtos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Tests.BuilderIo.Base;

public class Fixture : IAsyncDisposable
{
    private readonly BuilderIoClient client;

    public string ModelId { get; private set; } = "";

    public string ModelName { get; private set; } = "";

    public string ContentId { get; } = Guid.NewGuid().ToString("N");

    public string CallbackUrl { get; } = "https://example.invalid/blackbird-test/" + Guid.NewGuid().ToString("N");

    public Fixture(BuilderIoClient client)
    {
        this.client = client;
    }

    public async Task Initialize()
    {
        ModelName = "blackbird-test-" + Guid.NewGuid().ToString("N");
        var body = new
        {
            name = ModelName,
            kind = "data",
            fields = new object[]
            {
                new { name = "title", type = "string", localized = true },
                new { name = "body", type = "richText", localized = true },
                new { name = "tag", type = "tags" },
                new { name = "slug", type = "url" }
            }
        };
        var created = await client.Graph<Dictionary<string, ModelDto>>("mutation($body: JSONObject!) { addModel(body: $body) { id name kind fields } }", new { body });
        ModelId = created["addModel"].Id;
        await client.ExecuteWithErrorHandling(client.WriteRequest(ModelName, ContentId, Method.Put)
            .AddStringBody(JsonConvert.SerializeObject(new
            {
                name = "Blackbird round-trip test",
                published = "draft",
                data = new JObject
                {
                    ["title"] = new JObject { ["@type"] = "@builder.io/core:LocalizedValue", ["Default"] = "Hello world" },
                    ["body"] = new JObject { ["@type"] = "@builder.io/core:LocalizedValue", ["Default"] = "<p>Hello <strong>world</strong>.</p>" },
                    ["tag"] = new JArray("keep", "localize"),
                    ["slug"] = "https://example.com/source"
                }
            }), DataFormat.Json));
    }

    public async ValueTask DisposeAsync()
    {
        if (ModelId.Length > 0)
            await client.Graph<object>("mutation($id: String!) { deleteModel(id: $id) { id } }", new { id = ModelId });
    }
}
