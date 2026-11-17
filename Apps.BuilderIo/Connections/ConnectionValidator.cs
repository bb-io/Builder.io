using Apps.BuilderIo.Api;
using Apps.BuilderIo.Models.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.BuilderIo.Connections;

public class ConnectionValidator(InvocationContext context) : BaseInvocable(context), IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(IEnumerable<AuthenticationCredentialsProvider> credentials, CancellationToken cancellationToken)
    {
        try
        {
            var stored = credentials.ToList();
            if (stored.Any(x => x.KeyName == "access_token"))
            {
                using var projects = new ProjectsClient(stored);
                var identity = await projects.Call<Apps.BuilderIo.Models.Projects.ProjectsIdentityResponse>("Get authenticated user", new { });
                if (string.IsNullOrWhiteSpace(identity.Organization?.Id))
                    return new ConnectionValidationResponse { IsValid = false, Message = "Builder did not return an authorized organization. Reconnect or check Projects MCP compatibility." };
                return new ConnectionValidationResponse { IsValid = true, Message = "Projects organization connection verified. CMS actions require a separate Space API keys connection." };
            }
            var client = new BuilderIoClient(stored);
            var space = await client.Graph<SpaceDto>("query { id }");
            return new ConnectionValidationResponse
            {
                IsValid = space.Id == client.SpaceId,
                Message = space.Id == client.SpaceId ? "Connection verified. Write permissions are checked when actions run." : "The public and private API keys must belong to the same Builder Content Space."
            };
        }
        catch (Exception ex)
        {
            return new ConnectionValidationResponse { IsValid = false, Message = ex.Message };
        }
    }
}
