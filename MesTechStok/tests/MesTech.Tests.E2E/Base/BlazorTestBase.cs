using Microsoft.Playwright.NUnit;

namespace MesTech.Tests.E2E.Base;

/// <summary>
/// Base class for Blazor E2E tests using Playwright.
/// Configures browser, provides login helper, and sets base URLs.
///
/// Prerequisites:
///   - Blazor server running at BlazorBaseUrl (default: http://localhost:5200)
///   - WebApi running at ApiBaseUrl (default: http://localhost:5100)
///   - Docker services: PostgreSQL, Redis, RabbitMQ
/// </summary>
public class BlazorTestBase : PageTest
{
    protected string BlazorBaseUrl => Environment.GetEnvironmentVariable("E2E_BLAZOR_URL")
        ?? "http://localhost:5200";

    protected string ApiBaseUrl => Environment.GetEnvironmentVariable("E2E_API_URL")
        ?? "http://localhost:5100";

    protected string TestUsername => Environment.GetEnvironmentVariable("E2E_USERNAME")
        ?? "admin";

    protected string TestPassword => Environment.GetEnvironmentVariable("E2E_PASSWORD")
        ?? "Admin123!";

    /// <summary>
    /// Logs in via Blazor login page.
    /// </summary>
    protected async Task LoginAsync()
    {
        await Page.GotoAsync($"{BlazorBaseUrl}/giris");
        await Page.WaitForLoadStateAsync();

        var usernameInput = Page.Locator("input[placeholder='kullanici.adi'], input[name='username'], input[type='text']").First;
        var passwordInput = Page.Locator("input[type='password']").First;

        await usernameInput.FillAsync(TestUsername);
        await passwordInput.FillAsync(TestPassword);
        await Page.ClickAsync("button[type='submit']");

        await Page.WaitForURLAsync($"{BlazorBaseUrl}/**",
            new() { Timeout = 10000 });
    }

    /// <summary>
    /// Waits for page content to load (Blazor render cycle).
    /// </summary>
    protected async Task WaitForPageAsync()
    {
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);
    }
}
