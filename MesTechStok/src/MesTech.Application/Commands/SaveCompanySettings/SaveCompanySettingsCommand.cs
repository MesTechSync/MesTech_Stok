using MediatR;

namespace MesTech.Application.Commands.SaveCompanySettings;

public record SaveCompanySettingsCommand(
    string CompanyName,
    string? TaxNumber,
    string? Phone,
    string? Email,
    string? Address,
    List<WarehouseInput> Warehouses
) : IRequest<SaveCompanySettingsResult>;

public sealed class WarehouseInput
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
}

public sealed class SaveCompanySettingsResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
