using Bunit;
using FluentAssertions;
using MesTech.Blazor.Components.Shared;

namespace MesTech.Blazor.Tests;

public class ContentStateTests : TestContext
{
    [Fact]
    public void ShowsLoadingSpinner_WhenIsLoadingTrue()
    {
        var cut = RenderComponent<ContentState>(p => p
            .Add(cs => cs.IsLoading, true)
            .Add(cs => cs.HasError, false)
            .Add(cs => cs.IsEmpty, false));

        cut.Find(".spinner-border").Should().NotBeNull();
    }

    [Fact]
    public void ShowsErrorAlert_WhenHasErrorTrue()
    {
        var cut = RenderComponent<ContentState>(p => p
            .Add(cs => cs.IsLoading, false)
            .Add(cs => cs.HasError, true)
            .Add(cs => cs.ErrorMessage, "Test hatasi"));

        var alert = cut.Find("[role='alert']");
        alert.TextContent.Should().Contain("Test hatasi");
    }

    [Fact]
    public void ShowsDefaultErrorMessage_WhenErrorMessageEmpty()
    {
        var cut = RenderComponent<ContentState>(p => p
            .Add(cs => cs.IsLoading, false)
            .Add(cs => cs.HasError, true)
            .Add(cs => cs.ErrorMessage, ""));

        var alert = cut.Find("[role='alert']");
        alert.TextContent.Should().Contain("Beklenmeyen bir hata olustu");
    }

    [Fact]
    public void ShowsRetryButton_WhenOnRetryProvided()
    {
        var retried = false;
        var cut = RenderComponent<ContentState>(p => p
            .Add(cs => cs.IsLoading, false)
            .Add(cs => cs.HasError, true)
            .Add(cs => cs.ErrorMessage, "Hata")
            .Add(cs => cs.OnRetry, () => { retried = true; }));

        cut.Find("button").Click();
        retried.Should().BeTrue();
    }

    [Fact]
    public void ShowsEmptyState_WhenIsEmptyTrue()
    {
        var cut = RenderComponent<ContentState>(p => p
            .Add(cs => cs.IsLoading, false)
            .Add(cs => cs.HasError, false)
            .Add(cs => cs.IsEmpty, true)
            .Add(cs => cs.EmptyIcon, "fa-box-open")
            .Add(cs => cs.EmptyMessage, "Bos liste"));

        cut.Markup.Should().Contain("fa-box-open");
        cut.Markup.Should().Contain("Bos liste");
    }

    [Fact]
    public void ShowsChildContent_WhenAllFlagsNormal()
    {
        var cut = RenderComponent<ContentState>(p => p
            .Add(cs => cs.IsLoading, false)
            .Add(cs => cs.HasError, false)
            .Add(cs => cs.IsEmpty, false)
            .AddChildContent("<p>Gercek icerik</p>"));

        cut.Markup.Should().Contain("Gercek icerik");
    }

    [Fact]
    public void LoadingTakesPrecedence_OverError()
    {
        var cut = RenderComponent<ContentState>(p => p
            .Add(cs => cs.IsLoading, true)
            .Add(cs => cs.HasError, true));

        cut.Find(".spinner-border").Should().NotBeNull();
        cut.Markup.Should().NotContain("alert-danger");
    }

    [Fact]
    public void ErrorTakesPrecedence_OverEmpty()
    {
        var cut = RenderComponent<ContentState>(p => p
            .Add(cs => cs.IsLoading, false)
            .Add(cs => cs.HasError, true)
            .Add(cs => cs.IsEmpty, true)
            .Add(cs => cs.ErrorMessage, "Hata mesaji"));

        cut.Find("[role='alert']").TextContent.Should().Contain("Hata mesaji");
    }
}
