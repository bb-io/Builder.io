using Apps.BuilderIo.Connections;
using Blackbird.Applications.Sdk.Common.Authentication;
using Tests.BuilderIo.Base;

namespace Tests.BuilderIo;

[TestClass]
public class ConnectionValidatorTests : TestBase
{
    [TestMethod]
    public async Task ValidKeys_VerifySpaceIdentity()
    {
        var result = await new ConnectionValidator(Context).ValidateConnection(Creds, CancellationToken.None);
        Assert.IsTrue(result.IsValid, result.Message);
    }

    [TestMethod]
    public async Task InvalidKeys_ReturnReadableFailure()
    {
        var invalid = Creds.Select(c => new AuthenticationCredentialsProvider(c.KeyName, c.Value + "invalid"));
        var result = await new ConnectionValidator(Context).ValidateConnection(invalid, CancellationToken.None);
        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
    }
}
