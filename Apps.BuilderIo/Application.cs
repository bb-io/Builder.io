using Apps.BuilderIo.Auth.OAuth2;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication.OAuth2;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Metadata;

namespace Apps.BuilderIo;

public class Application(InvocationContext context) : BaseInvocable(context), IApplication, ICategoryProvider
{
    public string Name => "Builder.io";

    public IEnumerable<ApplicationCategory> Categories
    {
        get => [ApplicationCategory.Cms];
        set { }
    }

    public T GetInstance<T>()
    {
        if (typeof(T) == typeof(IOAuth2AuthorizeService))
            return (T)(object)new OAuth2AuthorizeService(InvocationContext);
        if (typeof(T) == typeof(IOAuth2TokenService))
            return (T)(object)new OAuth2TokenService(InvocationContext);
        throw new NotSupportedException($"Service {typeof(T).Name} is not supported.");
    }
}
