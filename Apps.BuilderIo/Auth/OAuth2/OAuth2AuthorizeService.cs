using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication.OAuth2;
using Blackbird.Applications.Sdk.Common.Invocation;
using Microsoft.AspNetCore.WebUtilities;

namespace Apps.BuilderIo.Auth.OAuth2;

public class OAuth2AuthorizeService(InvocationContext context) : BaseInvocable(context), IOAuth2AuthorizeService
{
    public string GetAuthorizationUrl(Dictionary<string, string> values)
    {
        var state = ProjectsOAuth.Required(values, "state");
        return QueryHelpers.AddQueryString("https://mcp.builder.io/oauth/authorize", new Dictionary<string, string?>
        {
            ["client_id"] = ProjectsOAuth.Required(values, ProjectsOAuth.ClientId),
            ["redirect_uri"] = InvocationContext.UriInfo.AuthorizationCodeRedirectUri.ToString(),
            ["response_type"] = "code",
            ["state"] = state,
            ["scope"] = ProjectsOAuth.Scopes,
            ["resource"] = ProjectsOAuth.Resource,
            ["code_challenge_method"] = "S256",
            ["code_challenge"] = ProjectsOAuth.Challenge(ProjectsOAuth.Verifier(values, state))
        });
    }
}
