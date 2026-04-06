using FluentAssertions;
using MesTech.Application.Features.AI.Commands.GenerateProductDescription;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GenerateProductDescriptionHandlerTests
{
    private readonly Mock<IMesaAIService> _mesaAI = new();
    private readonly Mock<ILogger<GenerateProductDescriptionHandler>> _logger = new();

    private GenerateProductDescriptionHandler CreateHandler() =>
        new(_mesaAI.Object, _logger.Object);

    [Fact]
    public async Task Handle_NullMesaAI_ShouldThrowOnUse()
    {
        var handler = new GenerateProductDescriptionHandler(null!, _logger.Object);
        var command = new GenerateProductDescriptionCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Product", null, null, null);
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task Handle_SuccessfulGeneration_ShouldReturnPopulatedResult()
    {
        var productId = Guid.NewGuid();
        var content = "Bu urun yuksek kaliteli malzemeden uretilmis olup dayanikli ve kullanimi kolaydir. Profesyonel kullanim icin idealdir. Uzun omurlu ve garanti kapsaminda.";
        var metadata = new Dictionary<string, string>
        {
            ["seo_title"] = "Test Product SEO Title",
            ["keywords"] = "kaliteli,dayanikli,profesyonel"
        };

        _mesaAI.Setup(m => m.GenerateProductDescriptionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiContentResult(true, content, null, metadata));

        var handler = CreateHandler();
        var command = new GenerateProductDescriptionCommand(
            productId, Guid.NewGuid(), "Test Product", "Elektronik", "BrandX", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ShortDescription.Should().HaveLength(100);
        result.LongDescription.Should().Be(content);
        result.SeoTitle.Should().Be("Test Product SEO Title");
        result.SeoKeywords.Should().BeEquivalentTo(new[] { "kaliteli", "dayanikli", "profesyonel" });
    }

    [Fact]
    public async Task Handle_FailedGeneration_ShouldReturnError()
    {
        _mesaAI.Setup(m => m.GenerateProductDescriptionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiContentResult(false, null, "AI service unavailable", null));

        var handler = CreateHandler();
        var command = new GenerateProductDescriptionCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Product", null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("AI service unavailable");
    }

    [Fact]
    public async Task Handle_ShortContent_ShouldNotTruncate()
    {
        var shortContent = "Kisa aciklama";
        _mesaAI.Setup(m => m.GenerateProductDescriptionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiContentResult(true, shortContent, null, null));

        var handler = CreateHandler();
        var command = new GenerateProductDescriptionCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Product", null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.ShortDescription.Should().Be(shortContent);
        result.MediumDescription.Should().Be(shortContent);
        result.LongDescription.Should().Be(shortContent);
    }

    [Fact]
    public async Task Handle_NoBrand_ShouldUseMesTechAsDefault()
    {
        var content = "Urun aciklamasi burada yer almaktadir ve oldukca detaylidir.";
        _mesaAI.Setup(m => m.GenerateProductDescriptionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiContentResult(true, content, null, null));

        var handler = CreateHandler();
        var command = new GenerateProductDescriptionCommand(
            Guid.NewGuid(), Guid.NewGuid(), "TestProduct", null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.SeoTitle.Should().Contain("MesTech");
    }

    [Fact]
    public async Task Handle_NoKeywordsMetadata_ShouldReturnEmptyKeywords()
    {
        _mesaAI.Setup(m => m.GenerateProductDescriptionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiContentResult(true, "Content", null, null));

        var handler = CreateHandler();
        var command = new GenerateProductDescriptionCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Product", null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.SeoKeywords.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldPassSkuAsFirst8CharsOfProductId()
    {
        var productId = Guid.NewGuid();
        var expectedSku = productId.ToString("N")[..8];

        _mesaAI.Setup(m => m.GenerateProductDescriptionAsync(
                expectedSku, "TestProd", "Cat1",
                It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiContentResult(true, "OK", null, null));

        var handler = CreateHandler();
        var command = new GenerateProductDescriptionCommand(
            productId, Guid.NewGuid(), "TestProd", "Cat1", null, null);

        await handler.Handle(command, CancellationToken.None);

        _mesaAI.Verify(m => m.GenerateProductDescriptionAsync(
            expectedSku, "TestProd", "Cat1",
            null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
