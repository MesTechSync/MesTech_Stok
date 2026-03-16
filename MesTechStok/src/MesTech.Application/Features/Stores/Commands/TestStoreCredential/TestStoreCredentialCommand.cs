using MediatR;

namespace MesTech.Application.Features.Stores.Commands.TestStoreCredential;

/// <summary>
/// Kaydedilmis credential'lar ile platform baglanti testi yapar.
/// DB'den credential cekilir, adapter factory ile uygun adapter resolve edilir,
/// TestConnectionAsync() cagrilir.
/// </summary>
public record TestStoreCredentialCommand(Guid StoreId) : IRequest<CredentialTestResult>;

public record CredentialTestResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int LatencyMs { get; init; }
    public string Platform { get; init; } = string.Empty;
}
