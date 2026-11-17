using Apps.BuilderIo.Constants;
using Apps.BuilderIo.Auth.OAuth2;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.BuilderIo.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups =>
    [
        new()
        {
            Name = "Space API keys",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties =
            [
                new(CredsNames.PublicKey) { DisplayName = "Public API key", Description = "The public API key of your Builder Content Space." },
                new(CredsNames.PrivateKey) { DisplayName = "Private API key", Sensitive = true, Description = "A private key for the same Space. Content writes and webhook setup require write and model permissions." }
            ]
        },
        new()
        {
            Name = "Projects (OAuth)",
            AuthenticationType = ConnectionAuthenticationType.OAuth2,
            ConnectionProperties =
            [
                new(ProjectsOAuth.ClientId) { DisplayName = "Projects OAuth client ID", Description = "Use the registered public Builder OAuth client for this Blackbird environment; see the OAuth setup guide." },
                new(ProjectsOAuth.PkceKey) { DisplayName = "PKCE signing key", Sensitive = true, Description = "The locally generated pkce_signing_key supplied with your client registration. This is not a Builder client secret and is never sent to Builder." }
            ]
        }
    ];

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(Dictionary<string, string> values)
        => values.Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value.Trim()));
}
