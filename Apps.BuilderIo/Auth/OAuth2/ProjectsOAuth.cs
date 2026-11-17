using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;

namespace Apps.BuilderIo.Auth.OAuth2;

public static class ProjectsOAuth
{
    public const string ClientId = "projects_client_id";
    public const string PkceKey = "projects_pkce_key";
    public const string Resource = "https://mcp.builder.io/mcp/fusion";
    public const string Scopes = "builder:projects:read builder:agents:run builder:ai:invoke offline_access";

    public static string Required(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new PluginMisconfigurationException($"Projects connection is missing {key}. Register an OAuth client and reconnect.");
        return value.Trim();
    }

    public static string Verifier(Dictionary<string, string> values, string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw new PluginMisconfigurationException("OAuth state is missing. Restart the connection process.");
        return Base64(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Required(values, PkceKey)), Encoding.UTF8.GetBytes("builder-projects-pkce:" + state)));
    }

    public static string Challenge(string verifier) => Base64(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    public static async Task<Dictionary<string, string>> Exchange(Dictionary<string, string> stored, Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        form["client_id"] = Required(stored, ClientId);
        form["resource"] = Resource;
        var body = await Post("https://mcp.builder.io/oauth/token", form, cancellationToken);
        JObject data;
        try
        {
            data = JObject.Parse(body);
        }
        catch (Newtonsoft.Json.JsonException)
        {
            throw new PluginApplicationException("Builder OAuth returned an unreadable token response. Reconnect and try again.");
        }
        var result = new Dictionary<string, string>(stored);
        foreach (var prop in data.Properties())
            if (prop.Value.Type != JTokenType.Null)
                result[prop.Name] = prop.Value.ToString();
        if (!result.TryGetValue("access_token", out var access) || string.IsNullOrWhiteSpace(access))
            throw new PluginApplicationException("Builder OAuth did not return an access token. Reconnect.");
        var validExpiry = double.TryParse(data["expires_in"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var expires);
        if (!validExpiry || !double.IsFinite(expires) || expires <= 0 || expires > (DateTimeOffset.MaxValue - DateTimeOffset.UtcNow).TotalSeconds)
            throw new PluginApplicationException("Builder OAuth did not return a valid token lifetime. Reconnect.");
        result["expires_at"] = DateTimeOffset.UtcNow.AddSeconds(expires).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        result.Remove("state");
        result.Remove("code");
        return result;
    }

    public static async Task<string> Post(string url, Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            using var response = await http.PostAsync(url, new FormUrlEncodedContent(form), cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new PluginMisconfigurationException($"Builder OAuth request failed (HTTP {(int)response.StatusCode}). Check client registration, exact redirect URI and organization access; reconnect if the authorization expired.");
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new PluginApplicationException("Could not reach Builder OAuth. Try connecting again later.");
        }
        catch (TaskCanceledException)
        {
            throw new PluginApplicationException("Builder OAuth timed out. The token exchange may have completed; reconnect if refresh credentials no longer work.");
        }
    }

    private static string Base64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
