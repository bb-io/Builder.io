using Apps.BuilderIo.Api;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Microsoft.Extensions.Configuration;

namespace Tests.BuilderIo.Base;

public class TestBase
{
    protected IEnumerable<AuthenticationCredentialsProvider> Creds { get; }

    protected InvocationContext Context { get; }

    protected BuilderIoClient Client => new(Creds);

    protected FileManager Files { get; } = new();

    protected TestBase(string connectionSection = "ConnectionDefinition")
    {
        var config = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: true).Build();
        Creds = config.GetSection(connectionSection).GetChildren()
            .Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value ?? "")).ToList();
        Context = new InvocationContext { AuthenticationCredentialsProviders = Creds };

    }
    [TestInitialize]
    public void RequireLiveCredentials()
    {
        if (!Creds.Any() || Creds.Any(x => string.IsNullOrWhiteSpace(x.Value)))
            Assert.Inconclusive("Supply the appropriate connection credentials in appsettings.json to run live integration tests.");
    }
}
