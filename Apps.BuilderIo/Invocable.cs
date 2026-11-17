using Apps.BuilderIo.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.BuilderIo;

public class Invocable : BaseInvocable
{
    protected BuilderIoClient Client { get; }

    public Invocable(InvocationContext context) : base(context)
    {
        Client = new BuilderIoClient(context.AuthenticationCredentialsProviders);
    }
}
