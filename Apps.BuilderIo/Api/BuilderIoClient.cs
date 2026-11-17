using System.Globalization;
using System.Net;
using Apps.BuilderIo.Constants;
using Apps.BuilderIo.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RestSharp;

namespace Apps.BuilderIo.Api;

public class BuilderIoClient : BlackBirdRestClient
{
    private readonly string privateKey;
    private readonly AsyncRetryPolicy<RestResponse> retry = Policy
        .HandleResult<RestResponse>(r => r.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(5, (attempt, result, _) => Delay(attempt, result.Result), (_, _, _, _) => Task.CompletedTask);

    public string SpaceId { get; }

    public BuilderIoClient(IEnumerable<AuthenticationCredentialsProvider> credentials) : base(new RestClientOptions
    {
        BaseUrl = new Uri("https://cdn.builder.io"),
        MaxTimeout = 180000
    })
    {
        var values = credentials.ToDictionary(x => x.KeyName, x => x.Value);
        if (values.ContainsKey("access_token"))
            throw new PluginMisconfigurationException("This is a CMS action. Select a Space API keys connection; Projects OAuth is for Projects actions.");
        SpaceId = Credential(values, CredsNames.PublicKey);
        privateKey = Credential(values, CredsNames.PrivateKey);
        this.AddDefaultHeader("Authorization", $"Bearer {privateKey}");
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        RestResponse response;
        try
        {
            response = await retry.ExecuteAsync(() => this.ExecuteAsync(request));
        }
        catch (HttpRequestException)
        {
            throw new PluginApplicationException("Could not reach Builder.io. Try again later.");
        }
        catch (TaskCanceledException)
        {
            throw new PluginApplicationException("Builder.io timed out. Try again later.");
        }
        if (!response.IsSuccessful)
            throw ConfigureErrorException(response);
        return response;
    }

    public new async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        var response = await ExecuteWithErrorHandling(request);
        try
        {
            return JsonConvert.DeserializeObject<T>(response.Content ?? "")
                ?? throw new PluginApplicationException("Builder.io returned an empty response. Try again later.");
        }
        catch (JsonException)
        {
            throw new PluginApplicationException("Builder.io returned an unreadable response. Try again later.");
        }
    }

    public async Task<T> Graph<T>(string query, object? variables = null)
    {
        var result = await ExecuteWithErrorHandling<GraphResponse<T>>(new RestRequest("/api/v2/admin", Method.Post)
            .AddStringBody(JsonConvert.SerializeObject(new { query, variables }), DataFormat.Json));
        if (result.Errors.Count > 0)
            throw new PluginMisconfigurationException("Builder.io Admin API: " + Safe(string.Join("; ", result.Errors.Select(x => x.Message))) + " Check the private key permissions and Space plan.");
        return result.Data ?? throw new PluginApplicationException("Builder.io returned no Admin API data. Try again later.");
    }

    public RestRequest ContentRequest(string model)
    {
        return new RestRequest($"/api/v3/content/{Uri.EscapeDataString(model)}")
            .AddQueryParameter("apiKey", SpaceId)
            .AddQueryParameter("includeUnpublished", "true")
            .AddQueryParameter("noTargeting", "true")
            .AddQueryParameter("enrich", "false")
            .AddQueryParameter("noTraverse", "true")
            .AddQueryParameter("cachebust", "true");
    }

    public async Task<List<T>> Paginate<T>(RestRequest request, int maximum = 10000)
    {
        var items = new List<T>();
        for (var offset = 0; offset <= maximum; offset += 100)
        {
            request.AddOrUpdateParameter("limit", "100", ParameterType.QueryString);
            request.AddOrUpdateParameter("offset", offset.ToString(CultureInfo.InvariantCulture), ParameterType.QueryString);
            var page = await ExecuteWithErrorHandling<ContentPage<T>>(request);
            items.AddRange(page.Results);
            if (items.Count > maximum)
                throw new PluginMisconfigurationException($"This search exceeds {maximum} results. Narrow the search or use a mapped content ID; no incomplete result was returned.");
            if (page.Results.Count < 100)
                return items;
        }
        throw new PluginMisconfigurationException("This search is too large. Narrow the search before trying again.");
    }

    public RestRequest WriteRequest(string model, string id, Method method)
        => new RestRequest($"https://builder.io/api/v1/write/{Uri.EscapeDataString(model)}/{Uri.EscapeDataString(id)}", method);

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var code = (int)response.StatusCode;
        var detail = code switch
        {
            401 or 403 => "Access denied. Check both API keys belong to this Space and the private key has the required permissions and plan access.",
            404 => "Content or model was not found. Check the selected model, content ID and connection.",
            429 => "The request limit was reached. Wait and retry, or reduce the frequency of this workflow.",
            0 => "Could not reach Builder.io. Try again later.",
            _ => $"Request failed ({code}). {Safe(response.Content ?? "Try again later.")}"
        };
        return code is >= 400 and < 500 && code != 429
            ? new PluginMisconfigurationException("Builder.io: " + detail)
            : new PluginApplicationException("Builder.io: " + detail);
    }

    private string Safe(string value)
    {
        var clean = value.Replace(privateKey, "[redacted]").Replace(SpaceId, "[space]");
        return clean.Length > 300 ? clean[..300] : clean;
    }

    private static string Credential(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new PluginMisconfigurationException("Fill in both the public and private API keys in the connection.");
        return value.Trim();
    }

    private static TimeSpan Delay(int attempt, RestResponse response)
    {
        var header = response.Headers?.FirstOrDefault(h => string.Equals(h.Name, "Retry-After", StringComparison.OrdinalIgnoreCase))?.Value?.ToString();
        if (double.TryParse(header, NumberStyles.Any, CultureInfo.InvariantCulture, out var seconds) && seconds > 0)
            return TimeSpan.FromSeconds(Math.Min(seconds, 120));
        if (DateTimeOffset.TryParse(header, out var date) && date > DateTimeOffset.UtcNow)
            return TimeSpan.FromSeconds(Math.Min((date - DateTimeOffset.UtcNow).TotalSeconds, 120));
        return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 30)) + TimeSpan.FromMilliseconds(Random.Shared.Next(500));
    }
}
