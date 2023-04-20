namespace Microsoft.Artifacts.Authentication.Tests;

// Tests are meant to be run manually as it requires interactive logins, multiple accounts, and domain joined machines.
[Ignore]
[TestClass]
public class MsalAuthenticationTests
{
    private static readonly Uri PackageUri = new Uri("https://pkgs.dev.azure.com/mseng/PipelineTools/_packaging/artifacts-credprovider/nuget/v3/index.json");
    private static readonly Uri AuthorityUri = new Uri("https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47");
    private static ILogger logger = LoggerFactory
        .Create(builder =>
        {
            builder.SetMinimumLevel(Extensions.Logging.LogLevel.Trace);
            builder.AddDebug();
            builder.AddConsole();
        })
        .CreateLogger(nameof(MsalAuthenticationTests));

    private IPublicClientApplication app = AzureArtifacts
        .CreateDefaultBuilder(AuthorityUri, logger)
        .WithBroker(true, logger)
        // The test hosting process (testhost.exe) may not have an associated console window, so use
        // the foreground window which is correct enough when debugging and running tests locally.
        .WithParentActivityOrWindow(() => GetForegroundWindow())
        .Build();

    [TestMethod]
    public async Task MsalAcquireTokenSilentTest()
    {
        var tokenProvider = new MsalSilentTokenProvider(app, logger);
        var tokenRequest = new TokenRequest(PackageUri);

        var result = await tokenProvider.GetTokenAsync(tokenRequest);

        Assert.IsNotNull(result);
        Assert.AreEqual(TokenSource.Broker, result.AuthenticationResultMetadata.TokenSource);
    }

    [TestMethod]
    public async Task MsalAcquireTokenByIntegratedWindowsAuthTest()
    {
        var tokenProvider = new MsalIntegratedWindowsAuthTokenProvider(app, logger);
        var tokenRequest = new TokenRequest(PackageUri);

        var result = await tokenProvider.GetTokenAsync(tokenRequest);

        Assert.IsNotNull(result);
        Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
    }

    [TestMethod]
    public async Task MsalAcquireTokenInteractiveTest()
    {
        var tokenProvider = new MsalInteractiveTokenProvider(app, logger);
        var tokenRequest = new TokenRequest(PackageUri);

        var result = await tokenProvider.GetTokenAsync(tokenRequest);

        Assert.IsNotNull(result);
        Assert.AreEqual(TokenSource.Broker, result.AuthenticationResultMetadata.TokenSource);
    }

    [TestMethod]
    public async Task MsalAcquireTokenWithDeviceCodeTest()
    {
        var tokenProvider = new MsalDeviceCodeTokenProvider(app, logger);
        var tokenRequest = new TokenRequest(PackageUri);

        var result = await tokenProvider.GetTokenAsync(tokenRequest);

        Assert.IsNotNull(result);
        Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
