using MediatR;

namespace MesTech.Application.Features.Stores.Commands.SaveStoreCredential;

/// <summary>
/// Magaza credential'larini kaydeder (upsert). Fields degerleri DB'ye sifreli yazilir.
/// </summary>
public record SaveStoreCredentialCommand : IRequest<Guid>
{
    public Guid StoreId { get; init; }
    public Guid TenantId { get; init; }
    public string Platform { get; init; } = string.Empty;

    /// <summary>
    /// Credential tipi: "api_key", "oauth2", "soap"
    /// </summary>
    public string CredentialType { get; init; } = string.Empty;

    /// <summary>
    /// Credential alanlari: { "ApiKey": "xxx", "Secret": "yyy" }
    /// Degerler handler tarafindan AES-256-GCM ile sifrelenir.
    /// </summary>
    public Dictionary<string, string> Fields { get; init; } = new();
}
