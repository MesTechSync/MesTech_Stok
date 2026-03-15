using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Dropshipping;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace MesTech.Tests.Integration.Dropshipping;

/// <summary>
/// ImageDownloadService WireMock entegrasyon testleri.
/// WireMockFixture: IClassFixture&lt;WireMockFixture&gt; — BaseUrl kullanılır (ServerUrl değil).
/// IHttpClientFactory: ServiceCollection.AddHttpClient("ImageDownloader") ile DI'dan alınır.
/// ENT-DROP-IMP-SPRINT-D — DEV 5 Task D-14
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "Dropshipping")]
public class ImageDownloadServiceTests : IClassFixture<WireMockFixture>
{
    private readonly WireMockFixture _fixture;
    private readonly ImageDownloadService _sut;

    // Küçük 2x2 JPEG test verisi (geçerli JPEG magic bytes ile)
    private static readonly byte[] FakeJpegBytes =
    {
        0xFF, 0xD8, 0xFF, 0xE0,          // JPEG SOI + APP0 marker
        0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
        0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
        0xFF, 0xD9                         // JPEG EOI
    };

    public ImageDownloadServiceTests(WireMockFixture fixture)
    {
        _fixture = fixture;

        // IHttpClientFactory: "ImageDownloader" named client — no base address
        // (ImageDownloadService uses full URLs from callers)
        var services = new ServiceCollection();
        services.AddHttpClient("ImageDownloader");

        var factory = services.BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();

        _sut = new ImageDownloadService(factory, NullLogger<ImageDownloadService>.Instance);
    }

    // ════════════════════════════════════════════════════════════════════
    // TEK RESIM İNDİRME
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DownloadSingleAsync_200Response_ReturnsDownloadedImage()
    {
        _fixture.Reset();

        _fixture.Server
            .Given(Request.Create().WithPath("/images/test.jpg").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "image/jpeg")
                .WithBody(FakeJpegBytes));

        var url = $"{_fixture.BaseUrl}/images/test.jpg";
        var result = await _sut.DownloadSingleAsync(url);

        result.Should().NotBeNull();
        result!.OriginalUrl.Should().Be(url);
        result.ContentType.Should().Contain("jpeg");
        result.SizeBytes.Should().Be(FakeJpegBytes.Length);
        result.Sha256Hash.Should().NotBeNullOrEmpty();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(FakeJpegBytes.Length);
    }

    [Fact]
    public async Task DownloadSingleAsync_404Response_ReturnsNull()
    {
        _fixture.Reset();

        _fixture.Server
            .Given(Request.Create().WithPath("/images/missing.jpg").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var url = $"{_fixture.BaseUrl}/images/missing.jpg";
        var result = await _sut.DownloadSingleAsync(url);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadSingleAsync_500Response_ReturnsNull()
    {
        _fixture.Reset();

        _fixture.Server
            .Given(Request.Create().WithPath("/images/error.jpg").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));

        var url = $"{_fixture.BaseUrl}/images/error.jpg";
        var result = await _sut.DownloadSingleAsync(url);

        result.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════════
    // TOPLU İNDİRME
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DownloadBatchAsync_TwoSuccessfulUrls_BothSucceeded()
    {
        _fixture.Reset();

        _fixture.Server
            .Given(Request.Create().WithPath("/batch/img1.jpg").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "image/jpeg")
                .WithBody(FakeJpegBytes));

        _fixture.Server
            .Given(Request.Create().WithPath("/batch/img2.jpg").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "image/jpeg")
                .WithBody(new byte[] { 0xFF, 0xD8, 0xAA, 0xBB, 0xFF, 0xD9 })); // different bytes

        var urls = new[]
        {
            $"{_fixture.BaseUrl}/batch/img1.jpg",
            $"{_fixture.BaseUrl}/batch/img2.jpg"
        };

        var result = await _sut.DownloadBatchAsync(urls, new ImageDownloadOptions());

        result.Succeeded.Count.Should().Be(2);
        result.Failed.Count.Should().Be(0);
        result.DuplicatesSkipped.Should().Be(0);
    }

    [Fact]
    public async Task DownloadBatchAsync_OneSuccessOneFail_CorrectCounts()
    {
        _fixture.Reset();

        _fixture.Server
            .Given(Request.Create().WithPath("/mixed/ok.jpg").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "image/jpeg")
                .WithBody(FakeJpegBytes));

        _fixture.Server
            .Given(Request.Create().WithPath("/mixed/fail.jpg").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var urls = new[]
        {
            $"{_fixture.BaseUrl}/mixed/ok.jpg",
            $"{_fixture.BaseUrl}/mixed/fail.jpg"
        };

        var result = await _sut.DownloadBatchAsync(urls, new ImageDownloadOptions(MaxRetries: 1));

        result.Succeeded.Count.Should().Be(1);
        result.Failed.Count.Should().Be(1);
    }

    [Fact]
    public async Task DownloadBatchAsync_DuplicateUrls_DownloadedOnce()
    {
        _fixture.Reset();

        _fixture.Server
            .Given(Request.Create().WithPath("/dedup/same.jpg").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "image/jpeg")
                .WithBody(FakeJpegBytes));

        var sameUrl = $"{_fixture.BaseUrl}/dedup/same.jpg";
        var urls = new[] { sameUrl, sameUrl, sameUrl };

        // IEnumerable.Distinct() removes URL-level duplicates before download
        var result = await _sut.DownloadBatchAsync(urls, new ImageDownloadOptions(DeduplicateByHash: true));

        // After Distinct(), only 1 URL → 1 succeeded
        result.Succeeded.Count.Should().Be(1);
        result.Failed.Count.Should().Be(0);
    }

    [Fact]
    public async Task DownloadBatchAsync_SameContentDifferentUrls_HashDeduplicated()
    {
        _fixture.Reset();

        // İki farklı URL, aynı içerik (aynı hash) → ikincisi duplicate sayılır
        _fixture.Server
            .Given(Request.Create().WithPath("/hash/img-a.jpg").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "image/jpeg")
                .WithBody(FakeJpegBytes));

        _fixture.Server
            .Given(Request.Create().WithPath("/hash/img-b.jpg").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "image/jpeg")
                .WithBody(FakeJpegBytes)); // same content → same hash

        var urls = new[]
        {
            $"{_fixture.BaseUrl}/hash/img-a.jpg",
            $"{_fixture.BaseUrl}/hash/img-b.jpg"
        };

        var result = await _sut.DownloadBatchAsync(urls,
            new ImageDownloadOptions(DeduplicateByHash: true));

        // 1 başarılı + 1 duplicate atlandı
        (result.Succeeded.Count + result.DuplicatesSkipped)
            .Should().Be(2);
        result.DuplicatesSkipped.Should().Be(1);
    }

    [Fact]
    public async Task DownloadBatchAsync_FileTooLarge_SkippedWithNullResult()
    {
        _fixture.Reset();

        // 100 byte dosya
        var smallPayload = new byte[100];
        new Random(42).NextBytes(smallPayload);

        _fixture.Server
            .Given(Request.Create().WithPath("/size/large.jpg").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "image/jpeg")
                .WithHeader("Content-Length", "100")
                .WithBody(smallPayload));

        var url = $"{_fixture.BaseUrl}/size/large.jpg";

        // MaxFileSizeBytes = 50 → 100 byte dosyayı reddeder (Content-Length header'a bakılır)
        var options = new ImageDownloadOptions(MaxFileSizeBytes: 50);
        var result = await _sut.DownloadBatchAsync(new[] { url }, options);

        result.Succeeded.Count.Should().Be(0);
    }

    [Fact]
    public async Task DownloadBatchAsync_EmptyUrlList_ReturnsEmptyResult()
    {
        _fixture.Reset();

        var result = await _sut.DownloadBatchAsync(
            Enumerable.Empty<string>(), new ImageDownloadOptions());

        result.Succeeded.Should().BeEmpty();
        result.Failed.Should().BeEmpty();
        result.DuplicatesSkipped.Should().Be(0);
    }
}
