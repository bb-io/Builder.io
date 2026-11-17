using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication.OAuth2;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.BuilderIo.Auth.OAuth2;

public class OAuth2TokenService(InvocationContext context) : BaseInvocable(context), IOAuth2TokenService
{
    public bool IsRefreshToken(Dictionary<string, string> values)
        => !long.TryParse(values.GetValueOrDefault("expires_at"), out var expiry) || expiry <= DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds();

    public Task<Dictionary<string, string>> RequestToken(string state, string code, Dictionary<string, string> values, CancellationToken cancellationToken)
        => ProjectsOAuth.Exchange(values, new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = InvocationContext.UriInfo.AuthorizationCodeRedirectUri.ToString(),
            ["code_verifier"] = ProjectsOAuth.Verifier(values, state)
        }, cancellationToken);

    public Task<Dictionary<string, string>> RefreshToken(Dictionary<string, string> values, CancellationToken cancellationToken)
        => ProjectsOAuth.Exchange(values, new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = ProjectsOAuth.Required(values, "refresh_token")
        }, cancellationToken);

    public async Task RevokeToken(Dictionary<string, string> values)
    {
        foreach (var key in new[] { "refresh_token", "access_token" })
        {
            if (!values.TryGetValue(key, out var token) || string.IsNullOrWhiteSpace(token))
                continue;
            await ProjectsOAuth.Post("https://mcp.builder.io/oauth/revoke", new Dictionary<string, string>
            {
                ["client_id"] = ProjectsOAuth.Required(values, ProjectsOAuth.ClientId),
                ["token"] = token,
                ["token_type_hint"] = key
            }, CancellationToken.None);
        }
    }
}
