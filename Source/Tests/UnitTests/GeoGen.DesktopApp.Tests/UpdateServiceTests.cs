using GeoGen.DesktopApp.Services;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace GeoGen.DesktopApp.Tests;

public sealed class UpdateServiceTests
{
    private const string Repo = "https://github.com/nikolaveselinov/Planar-Geometry-Studio";

    [TestCase("2.0.0", "1.9.9", true)]
    [TestCase("1.10.0", "1.9.0", true)]
    [TestCase("v1.2.0", "1.2.0-rc.1", true)]
    [TestCase("1.2.0-rc.10", "1.2.0-rc.2", true)]
    [TestCase("1.2.0-alpha.2", "1.2.0-alpha.1", true)]
    [TestCase("1.2.0-beta", "1.2.0-alpha.10", true)]
    [TestCase("1.2.0", "1.2.0+build.27", false)]
    [TestCase("1.2.0-beta", "1.2.0", false)]
    [TestCase("1.1.0", "1.2.0", false)]
    [TestCase("not-a-version", "1.2.0", false)]
    [TestCase("1.2.0", "development", false)]
    [TestCase("01.2.0", "1.2.0", false)]
    [TestCase("1.2.0-rc.01", "1.2.0-rc.0", false)]
    public void OrdersSemanticVersions(string candidate, string current, bool expected) =>
        Assert.That(ReleaseVersion.IsNewer(candidate, current), Is.EqualTo(expected));

    [TestCase("win-x64", "-setup.exe")]
    [TestCase("win-arm64", "-setup.exe")]
    [TestCase("osx-x64", ".pkg")]
    [TestCase("osx-arm64", ".pkg")]
    [TestCase("linux-x64", ".run")]
    [TestCase("linux-arm64", ".run")]
    public async Task SelectsNativeInstallerForArchitecture(string rid, string suffix)
    {
        var filename = $"PlanarGeometryStudio-v1.2.0-{rid}{suffix}";
        var url = $"{Repo}/releases/download/v1.2.0/{filename}";
        using var client = CreateClient(Release(filename, url));
        var result = await new UpdateService(client).CheckAsync("1.1.2", rid);
        Assert.Multiple(() =>
        {
            Assert.That(result.Release?.Version, Is.EqualTo("1.2.0"));
            Assert.That(result.Release?.InstallerUrl?.AbsoluteUri, Is.EqualTo(url));
            Assert.That(result.Release?.ReleasePage.AbsoluteUri, Is.EqualTo(Repo + "/releases/tag/v1.2.0"));
        });
    }

    [Test]
    public async Task RejectsInstallerFromAnotherHostOrRepository()
    {
        using var client = CreateClient(Release("PlanarGeometryStudio-v1.2.0-win-x64-setup.exe", "https://example.org/setup.exe"));
        var result = await new UpdateService(client).CheckAsync("1.1.2", "win-x64");
        Assert.That(result.Release, Is.Not.Null);
        Assert.That(result.Release!.InstallerUrl, Is.Null);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task IgnoresDraftAndPrerelease(bool draft, bool prerelease)
    {
        using var client = CreateClient(Release("", "", draft, prerelease));
        Assert.That((await new UpdateService(client).CheckAsync("1.1.2", "win-x64")).Release, Is.Null);
    }

    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.TooManyRequests)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    public async Task HandlesUnavailableApi(HttpStatusCode status)
    {
        using var client = CreateClient("", status);
        var result = await new UpdateService(client).CheckAsync("1.1.2", "linux-x64");
        Assert.That(result.Release, Is.Null);
        Assert.That(result.Message, Is.Not.Empty);
    }

    [TestCase("not json")]
    [TestCase("[]")]
    [TestCase("{\"tag_name\":42}")]
    [TestCase("{\"draft\":false,\"prerelease\":false,\"tag_name\":\"invalid\"}")]
    public async Task HandlesMalformedResponses(string json)
    {
        using var client = CreateClient(json);
        Assert.That((await new UpdateService(client).CheckAsync("1.1.2", "linux-x64")).Release, Is.Null);
    }

    [Test]
    public async Task BoundsResponseSize()
    {
        using var client = CreateClient(new string('x', 1_048_577));
        var result = await new UpdateService(client).CheckAsync("1.1.2", "linux-x64");
        Assert.That(result.Message, Does.Contain("too large"));
    }

    [Test]
    public void PropagatesCallerCancellation()
    {
        using var client = new HttpClient(new FakeHandler((_, token) => Task.FromCanceled<HttpResponseMessage>(token)));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.CatchAsync<OperationCanceledException>(async () =>
            await new UpdateService(client).CheckAsync("1.1.2", "linux-x64", cancellation.Token));
    }

    [Test]
    public async Task SendsOnlyPublicReleaseRequestWithVersionHeader()
    {
        using var client = new HttpClient(new FakeHandler((request, _) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(request.RequestUri!.AbsoluteUri, Is.EqualTo("https://api.github.com/repos/nikolaveselinov/Planar-Geometry-Studio/releases/latest"));
                Assert.That(request.Headers.UserAgent.ToString(), Is.EqualTo("PlanarGeometryStudio/1.1.2"));
                Assert.That(request.Content, Is.Null);
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }));
        await new UpdateService(client).CheckAsync("1.1.2", "linux-x64");
    }

    private static string Release(string name, string url, bool draft = false, bool prerelease = false) =>
        JsonSerializer.Serialize(new
        {
            tag_name = "v1.2.0", draft, prerelease,
            assets = new[] { new { name, browser_download_url = url } }
        });

    private static HttpClient CreateClient(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        })));

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request, cancellationToken);
    }
}
