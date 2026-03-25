namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Counterparty data transfer object.
/// </summary>
public sealed class CounterpartyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? VKN { get; set; }
    public string CounterpartyType { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Platform { get; set; }
    public bool IsActive { get; set; }
}
