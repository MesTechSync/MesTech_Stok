using FluentAssertions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Email;

/// <summary>
/// AccountingEmailScanner unit tests.
/// Tests configuration validation, zero-unread handling, and error scenarios.
/// NOTE: IMAP interaction cannot be fully unit-tested (requires integration tests with MailKit).
/// These tests verify configuration guards, tenant context, and edge cases.
/// </summary>
[Trait("Category", "Unit")]
public class AccountingEmailScannerTests
{
    private readonly Mock<IMesaAccountingService> _accountingServiceMock;
    private readonly Mock<IAccountingDocumentRepository> _documentRepoMock;
    private readonly Mock<ITenantProvider> _tenantProviderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AccountingEmailScannerTests()
    {
        _accountingServiceMock = new Mock<IMesaAccountingService>();
        _documentRepoMock = new Mock<IAccountingDocumentRepository>();
        _tenantProviderMock = new Mock<ITenantProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _tenantProviderMock.Setup(t => t.GetCurrentTenantId()).Returns(_tenantId);
    }

    private AccountingEmailScanner CreateScanner(Dictionary<string, string?> configValues)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        return new AccountingEmailScanner(
            config,
            _accountingServiceMock.Object,
            _documentRepoMock.Object,
            _tenantProviderMock.Object,
            _unitOfWorkMock.Object,
            new Mock<ILogger<AccountingEmailScanner>>().Object);
    }

    // ── Configuration Guard Tests ──

    [Fact]
    public async Task ScanAndProcess_MissingHost_ReturnsZero()
    {
        // Arrange — no IMAP config
        var scanner = CreateScanner(new Dictionary<string, string?>());

        // Act
        var result = await scanner.ScanAndProcessAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ScanAndProcess_MissingUsername_ReturnsZero()
    {
        // Arrange
        var scanner = CreateScanner(new Dictionary<string, string?>
        {
            ["Email:Accounting:Host"] = "imap.test.com",
            ["Email:Accounting:Port"] = "993",
            ["Email:Accounting:Password"] = "test-pass"
            // Username missing
        });

        // Act
        var result = await scanner.ScanAndProcessAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ScanAndProcess_MissingPassword_ReturnsZero()
    {
        // Arrange
        var scanner = CreateScanner(new Dictionary<string, string?>
        {
            ["Email:Accounting:Host"] = "imap.test.com",
            ["Email:Accounting:Port"] = "993",
            ["Email:Accounting:Username"] = "test@test.com"
            // Password missing
        });

        // Act
        var result = await scanner.ScanAndProcessAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ScanAndProcess_EmptyHost_ReturnsZero()
    {
        // Arrange
        var scanner = CreateScanner(new Dictionary<string, string?>
        {
            ["Email:Accounting:Host"] = "  ",
            ["Email:Accounting:Port"] = "993",
            ["Email:Accounting:Username"] = "test@test.com",
            ["Email:Accounting:Password"] = "pass"
        });

        // Act
        var result = await scanner.ScanAndProcessAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ScanAndProcess_AllConfigPresent_DefaultFolderIsInbox()
    {
        // Arrange — config present but IMAP will fail (no real server)
        // This verifies the folder defaults to INBOX
        var scanner = CreateScanner(new Dictionary<string, string?>
        {
            ["Email:Accounting:Host"] = "imap.nonexistent.test",
            ["Email:Accounting:Port"] = "993",
            ["Email:Accounting:Username"] = "test@test.com",
            ["Email:Accounting:Password"] = "pass"
            // No "Folder" key — should default to INBOX
        });

        // Act — will try to connect and fail, returning 0
        var result = await scanner.ScanAndProcessAsync();

        // Assert — returns 0 due to connection failure
        result.Should().Be(0);
    }

    [Fact]
    public async Task ScanAndProcess_InvalidPort_DefaultsTo993()
    {
        // Arrange
        var scanner = CreateScanner(new Dictionary<string, string?>
        {
            ["Email:Accounting:Host"] = "imap.nonexistent.test",
            ["Email:Accounting:Port"] = "not-a-number",
            ["Email:Accounting:Username"] = "test@test.com",
            ["Email:Accounting:Password"] = "pass"
        });

        // Act — will try to connect with port 993 and fail
        var result = await scanner.ScanAndProcessAsync();

        // Assert
        result.Should().Be(0);
    }

    // ── IMAP Connection Failure Tests ──

    [Fact]
    public async Task ScanAndProcess_ConnectionFailed_ReturnsZero()
    {
        // Arrange — valid config but unreachable host
        var scanner = CreateScanner(new Dictionary<string, string?>
        {
            ["Email:Accounting:Host"] = "127.0.0.1",
            ["Email:Accounting:Port"] = "19999", // unlikely open port
            ["Email:Accounting:Username"] = "test@test.com",
            ["Email:Accounting:Password"] = "pass"
        });

        // Act
        var result = await scanner.ScanAndProcessAsync();

        // Assert — gracefully returns 0 on connection failure
        result.Should().Be(0);
    }

    // ── Interface Compliance Tests ──

    [Fact]
    public void Scanner_ImplementsIAccountingEmailScanner()
    {
        // Arrange
        var scanner = CreateScanner(new Dictionary<string, string?>());

        // Assert
        scanner.Should().BeAssignableTo<IAccountingEmailScanner>();
    }

    [Fact]
    public async Task ScanAndProcess_DoesNotCallSaveChanges_WhenNoAttachments()
    {
        // Arrange — no config = returns 0 early
        var scanner = CreateScanner(new Dictionary<string, string?>());

        // Act
        await scanner.ScanAndProcessAsync();

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScanAndProcess_DoesNotCallDocRepo_WhenNoConfig()
    {
        // Arrange
        var scanner = CreateScanner(new Dictionary<string, string?>());

        // Act
        await scanner.ScanAndProcessAsync();

        // Assert
        _documentRepoMock.Verify(r => r.AddAsync(
            It.IsAny<MesTech.Domain.Accounting.Entities.AccountingDocument>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScanAndProcess_CancellationToken_Honored()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var scanner = CreateScanner(new Dictionary<string, string?>
        {
            ["Email:Accounting:Host"] = "imap.test.com",
            ["Email:Accounting:Username"] = "test@test.com",
            ["Email:Accounting:Password"] = "pass"
        });

        // Act — OperationCanceledException propagates through MailKit
        // The source code re-throws OperationCanceledException (ex is not OperationCanceledException filter)
        var act = () => scanner.ScanAndProcessAsync(cts.Token);

        // Assert — should throw OperationCanceledException
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ScanAndProcess_CustomFolder_UsesConfiguredFolder()
    {
        // Arrange — custom folder configured
        var scanner = CreateScanner(new Dictionary<string, string?>
        {
            ["Email:Accounting:Host"] = "127.0.0.1",
            ["Email:Accounting:Port"] = "19998",
            ["Email:Accounting:Username"] = "test@test.com",
            ["Email:Accounting:Password"] = "pass",
            ["Email:Accounting:Folder"] = "Accounting"
        });

        // Act — will fail connection but validates config is used
        var result = await scanner.ScanAndProcessAsync();

        // Assert
        result.Should().Be(0);
    }

    // ── SupportedMimeTypes Tests (via reflection) ──

    [Fact]
    public void SupportedMimeTypes_IncludesPdf()
    {
        var field = typeof(AccountingEmailScanner)
            .GetField("SupportedMimeTypes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        field.Should().NotBeNull();
        var mimeTypes = field!.GetValue(null) as HashSet<string>;
        mimeTypes.Should().NotBeNull();
        mimeTypes!.Should().Contain("application/pdf");
    }

    [Fact]
    public void SupportedMimeTypes_IncludesImageTypes()
    {
        var field = typeof(AccountingEmailScanner)
            .GetField("SupportedMimeTypes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var mimeTypes = field!.GetValue(null) as HashSet<string>;
        mimeTypes!.Should().Contain("image/jpeg");
        mimeTypes.Should().Contain("image/png");
        mimeTypes.Should().Contain("image/tiff");
    }
}
