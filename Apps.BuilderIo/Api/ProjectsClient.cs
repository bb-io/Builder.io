using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Apps.BuilderIo.Auth.OAuth2;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.BuilderIo.Api;

public sealed class ProjectsClient : IDisposable
{
    private readonly HttpClient http;
    private readonly List<JObject> tools = [];
    private bool initialized;
    private int sequence;

    public ProjectsClient(IEnumerable<AuthenticationCredentialsProvider> credentials)
    {
        var token = credentials.FirstOrDefault(x => x.KeyName == "access_token")?.Value;
        if (string.IsNullOrWhiteSpace(token))
            throw new PluginMisconfigurationException("Select a Projects (OAuth) connection for this action. Space API keys only support CMS content.");
        http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/event-stream");
    }

    public async Task<T> Call<T>(string operation, object arguments)
    {
        try
        {
            await Initialize();
            var matches = tools.Where(t => Matches(t, operation)).ToList();
            if (matches.Count != 1)
                throw new PluginApplicationException(CatalogueDiagnostic(operation, matches.Count));
            var tool = matches[0];
            var args = JObject.FromObject(arguments, JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            var schema = tool["inputSchema"] as JObject ?? throw new PluginApplicationException("Builder omitted its tool input schema. This version cannot safely invoke that tool.");
            foreach (var key in schema?["required"]?.Values<string>() ?? [])
                if (key != null && !args.ContainsKey(key))
                    throw new PluginApplicationException($"Builder's '{operation}' tool requires '{key}', which this app version does not support. Update the app.");
            if (schema?["properties"] is JObject properties && args.Properties().Any(p => !properties.ContainsKey(p.Name)))
                throw new PluginApplicationException($"Builder's '{operation}' input schema is incompatible with this app version. Update the app.");
            var result = await Rpc("tools/call", new { name = tool["name"]!.Value<string>(), arguments = args });
            if (result["isError"]?.Value<bool>() == true)
                throw new PluginMisconfigurationException($"Builder could not complete '{operation}'. Check project/branch access, input values, agent credits and required permissions. Inspect the run in Builder before retrying a submission.");
            var payload = result["structuredContent"];
            if (payload == null)
            {
                var texts = result["content"]?.Where(x => x["type"]?.Value<string>() == "text").Select(x => x["text"]?.Value<string>()).ToList() ?? [];
                foreach (var text in texts)
                {
                    try
                    {
                        payload = JToken.Parse(text ?? "");
                        break;
                    }
                    catch (JsonException) { }
                }
            }
            if (payload is not JObject)
                throw new PluginApplicationException($"Builder returned no structured JSON for '{operation}'. This app cannot safely interpret a prose-only response. Check Builder MCP compatibility.");
            return payload.ToObject<T>() ?? throw new PluginApplicationException("Builder returned an empty Projects response.");
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidCastException or OverflowException)
        {
            throw new PluginApplicationException("Builder returned an incompatible Projects response. Update the app or contact support.");
        }
    }

    public async Task<List<T>> Paginate<T>(string operation, string collection, object arguments, int maximum)
    {
        var args = JObject.FromObject(arguments, JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        var items = new List<T>();
        var offset = 0;
        for (var page = 0; page < 100; page++)
        {
            args["offset"] = offset;
            args["limit"] = 100;
            var data = await Call<JObject>(operation, args);
            if (data[collection] is not JArray array || data["hasMore"]?.Type != JTokenType.Boolean)
                throw new PluginApplicationException("Builder omitted structured pagination information. Search completeness cannot be verified.");
            items.AddRange(array.ToObject<List<T>>() ?? []);
            var more = data["hasMore"]!.Value<bool>();
            if (items.Count > maximum || (items.Count == maximum && more))
                throw new PluginMisconfigurationException($"More than {maximum} projects match. Narrow the search to retrieve complete results.");
            if (!more)
                return items;
            var next = data["nextOffset"]?.Value<int?>();
            if (next == null || next <= offset)
                throw new PluginApplicationException("Builder returned incomplete project pagination. Narrow the search or try again later.");
            offset = next.Value;
        }
        throw new PluginMisconfigurationException("Project search exceeded 100 requests. Narrow the search.");
    }

    public void Dispose() => http.Dispose();

    private async Task Initialize()
    {
        if (initialized)
            return;
        var info = await Rpc("initialize", new { protocolVersion = "2025-03-26", capabilities = new { }, clientInfo = new { name = "Blackbird Builder.io", version = "1.1.3" } });
        var version = info["protocolVersion"]?.Value<string>();
        if (version is not ("2025-03-26" or "2025-06-18" or "2025-11-25"))
            throw new PluginApplicationException("Builder negotiated an unsupported MCP protocol version. Update the app.");
        http.DefaultRequestHeaders.TryAddWithoutValidation("MCP-Protocol-Version", version);
        await Send(new { jsonrpc = "2.0", method = "notifications/initialized" }, null);
        string? cursor = null;
        var seen = new HashSet<string>();
        for (var page = 0; page < 20; page++)
        {
            var result = await Rpc("tools/list", cursor == null ? new { } : (object)new { cursor });
            if (result["tools"] is not JArray catalogue)
                throw new PluginApplicationException("Builder returned an unreadable tool catalogue.");
            tools.AddRange(catalogue.OfType<JObject>());
            cursor = result["nextCursor"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(cursor))
            {
                initialized = true;
                return;
            }
            if (!seen.Add(cursor))
                break;
        }
        throw new PluginApplicationException("Builder's tool catalogue is incomplete. Try again later or contact support.");
    }

    private Task<JObject> Rpc(string method, object parameters)
    {
        var id = ++sequence;
        return Send(new { jsonrpc = "2.0", id, method, @params = parameters }, id);
    }

    private async Task<JObject> Send(object body, int? id)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ProjectsOAuth.Resource)
            {
                Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            };
            using var response = await http.SendAsync(request);
            if ((int)response.StatusCode is 401 or 403)
                throw new PluginMisconfigurationException("Builder Projects access was denied. Reconnect using Projects (OAuth), select the correct organization and verify project and agent permissions.");
            if ((int)response.StatusCode == 429)
                throw new PluginApplicationException("Builder Projects rate limit reached. Wait before retrying. For agent submissions, reuse the same idempotency key.");
            if (!response.IsSuccessStatusCode)
                throw new PluginApplicationException($"Builder Projects request failed (HTTP {(int)response.StatusCode}). Check Builder before retrying an agent submission; it may already have started.");
            if (response.Headers.TryGetValues("Mcp-Session-Id", out var sessions))
            {
                http.DefaultRequestHeaders.Remove("Mcp-Session-Id");
                http.DefaultRequestHeaders.TryAddWithoutValidation("Mcp-Session-Id", sessions.First());
            }
            if (id == null)
                return new JObject();
            var text = await response.Content.ReadAsStringAsync();
            JObject? envelope = null;
            if (response.Content.Headers.ContentType?.MediaType == "text/event-stream")
            {
                foreach (var block in text.Replace("\r\n", "\n").Split("\n\n"))
                {
                    var data = string.Join("\n", block.Split('\n').Where(l => l.StartsWith("data:")).Select(l => l[5..].TrimStart()));
                    if (string.IsNullOrWhiteSpace(data))
                        continue;
                    var candidate = JObject.Parse(data);
                    if (candidate["id"]?.ToString() == id.ToString())
                        envelope = candidate;
                }
            }
            else
                envelope = JObject.Parse(text);
            if (envelope == null || envelope["id"]?.ToString() != id.ToString())
                throw new PluginApplicationException("Builder returned no matching MCP response. Check the task in Builder before retrying.");
            if (envelope["error"] != null)
                throw new PluginApplicationException("Builder rejected the Projects operation. Check permissions and app compatibility. No automatic retry was performed.");
            return envelope["result"] as JObject ?? throw new PluginApplicationException("Builder returned an empty MCP result.");
        }
        catch (HttpRequestException)
        {
            throw new PluginApplicationException("Could not reach Builder Projects. Check Builder before retrying an agent submission and reuse its idempotency key.");
        }
        catch (TaskCanceledException)
        {
            throw new PluginApplicationException("Builder Projects timed out. The agent task may still have started. Check Builder before retrying and reuse its idempotency key.");
        }
        catch (JsonException)
        {
            throw new PluginApplicationException("Builder returned unreadable MCP data. Check app compatibility before retrying.");
        }
    }

    private string CatalogueDiagnostic(string operation, int matchCount)
    {
        var entries = tools.Take(50).Select(tool =>
        {
            var name = DiagnosticLabel(tool["name"]?.Value<string>());
            var title = DiagnosticLabel(tool["title"]?.Value<string>() ?? tool["annotations"]?["title"]?.Value<string>());
            return string.IsNullOrEmpty(title) ? name : $"{name} [{title}]";
        });
        var catalogue = tools.Count == 0 ? "(empty catalogue)" : string.Join("; ", entries);
        var remaining = tools.Count > 50 ? $" Showing first 50 of {tools.Count}." : "";
        return $"Builder Projects tool mapping failed for '{operation}': {matchCount} matches among {tools.Count} advertised tools. Advertised names/titles: {catalogue}.{remaining} Share this diagnostic with app support. Authentication reached tool discovery; this message does not establish a permissions failure. No tool was invoked for this operation.";
    }

    private static string DiagnosticLabel(string? value)
    {
        var clean = Regex.Replace(value ?? "", "[^a-zA-Z0-9 _.:/-]", "");
        return clean.Length > 100 ? clean[..100] + "..." : clean;
    }

    private static bool Matches(JObject tool, string operation)
    {
        if (operation == "Get authenticated user" && tool["name"]?.Value<string>() == "who_am_i")
            return true;
        var wanted = Normalize(operation);
        return new[] { tool["name"]?.Value<string>(), tool["title"]?.Value<string>(), tool["annotations"]?["title"]?.Value<string>() }
            .Where(x => x != null).Any(x => Normalize(x!) == wanted);
    }

    private static string Normalize(string value)
    {
        value = Regex.Replace(value, "([a-z])([A-Z])", "$1 $2").ToLowerInvariant();
        var words = Regex.Split(value, "[^a-z0-9]+").Where(x => x.Length > 0 && x is not ("a" or "the" or "builder" or "code" or "fusion"));
        return string.Join("", words);
    }
}
