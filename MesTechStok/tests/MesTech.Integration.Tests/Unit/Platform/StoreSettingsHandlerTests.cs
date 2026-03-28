using FluentAssertions;
using MesTech.Application.Features.Platform.Commands.TestStoreConnection;
using MesTech.Application.Features.Settings.Queries.GetStoreSettings;
using MesTech.Application.Features.Settings.Commands.UpdateStoreSettings;
using MesTech.Application.Features.Platform.Commands.SaveStoreCredential;
using MesTech.Application.Features.Platform.Commands.TestStoreCredential;
using MesTech.Application.Features.Platform.Commands.DeleteStoreCredential;
using MesTech.Application.Features.Platform.Queries.GetCredentialsSettings;
using MesTech.Application.Features.Barcode.Commands.CreateBarcodeScanLog;
using MesTech.Application.Features.Barcode.Queries.GetBarcodeScanLogs;
using MesTech.Application.Features.System.Logs.Commands.CreateLogEntry;
using MesTech.Application.Features.System.Logs.Queries.GetLogs;
using MesTech.Application.Features.System.Logs.Queries.GetLogCount;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

[Trait("Category", "Unit")]
[Trait("Layer", "Settings")]
[Trait("Group", "Handler-Extended")]
public class StoreSettingsHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    public StoreSettingsHandlerTests() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    // ═══ STORE ═══
    [Fact] public async Task TestStoreConnection_Null_Throws() { var sr = new Mock<IStoreRepository>(); var cr = new Mock<IStoreCredentialRepository>(); var enc = new Mock<ICredentialEncryptionService>(); var af = new Mock<IAdapterFactory>(); var h = new TestStoreConnectionHandler(sr.Object, cr.Object, enc.Object, af.Object, Mock.Of<ILogger<TestStoreConnectionHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetStoreSettings_Null_Throws() { var cs = new Mock<ICompanySettingsRepository>(); var sr = new Mock<IStoreRepository>(); var h = new GetStoreSettingsHandler(cs.Object, sr.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task UpdateStoreSettings_Null_Throws() { var cs = new Mock<ICompanySettingsRepository>(); var h = new UpdateStoreSettingsHandler(cs.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task SaveStoreCredential_Null_Throws() { var sr = new Mock<IStoreRepository>(); var cr = new Mock<IStoreCredentialRepository>(); var enc = new Mock<ICredentialEncryptionService>(); var h = new SaveStoreCredentialHandler(sr.Object, cr.Object, _uow.Object, enc.Object, Mock.Of<ILogger<SaveStoreCredentialHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task TestStoreCredential_Null_Throws() { var sr = new Mock<IStoreRepository>(); var cr = new Mock<IStoreCredentialRepository>(); var enc = new Mock<ICredentialEncryptionService>(); var af = new Mock<IAdapterFactory>(); var h = new TestStoreCredentialHandler(sr.Object, cr.Object, enc.Object, af.Object, Mock.Of<ILogger<TestStoreCredentialHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task DeleteStoreCredential_Null_Throws() { var cr = new Mock<IStoreCredentialRepository>(); var h = new DeleteStoreCredentialHandler(cr.Object, _uow.Object, Mock.Of<ILogger<DeleteStoreCredentialHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetCredentialsSettings_Null_Throws() { var sr = new Mock<IStoreRepository>(); var h = new GetCredentialsSettingsHandler(sr.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ BARCODE ═══
    [Fact] public async Task CreateBarcodeScanLog_Null_Throws() { var r = new Mock<IBarcodeScanLogRepository>(); var h = new CreateBarcodeScanLogHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetBarcodeScanLogs_Null_Throws() { var r = new Mock<IBarcodeScanLogRepository>(); var h = new GetBarcodeScanLogsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ LOG ═══
    [Fact] public async Task CreateLogEntry_Null_Throws() { var r = new Mock<ILogEntryRepository>(); var h = new CreateLogEntryHandler(r.Object, _uow.Object, Mock.Of<ILogger<CreateLogEntryHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetLogs_Null_Throws() { var r = new Mock<ILogEntryRepository>(); var h = new GetLogsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetLogCount_Null_Throws() { var r = new Mock<ILogEntryRepository>(); var h = new GetLogCountHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
