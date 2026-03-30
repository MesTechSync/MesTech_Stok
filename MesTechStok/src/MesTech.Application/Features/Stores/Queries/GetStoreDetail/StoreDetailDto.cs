namespace MesTech.Application.Features.Stores.Queries.GetStoreDetail;

public sealed class StoreDetailDto
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public int ProductCount { get; set; }
    public string CredentialStatus { get; set; } = string.Empty;
    public string WebhookStatus { get; set; } = string.Empty;
}
