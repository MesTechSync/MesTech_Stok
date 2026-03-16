using MediatR;

namespace MesTech.Application.Features.Stores.Queries.GetStoreCredential;

/// <summary>
/// Magaza credential'larini maskelenmis olarak dondurur.
/// Degerler asla plaintext donmez — sadece alan adlari ve maskelenmis degerler.
/// </summary>
public record GetStoreCredentialQuery(Guid StoreId) : IRequest<StoreCredentialDto?>;

public record StoreCredentialDto
{
    public Guid StoreId { get; init; }
    public string Platform { get; init; } = string.Empty;
    public string CredentialType { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> MaskedFields { get; init; }
        = new Dictionary<string, string>();
    public DateTime LastUpdated { get; init; }
}
