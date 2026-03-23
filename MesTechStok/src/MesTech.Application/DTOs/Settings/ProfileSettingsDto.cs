namespace MesTech.Application.DTOs.Settings;

public record ProfileSettingsDto(string TenantName, string? TaxNumber, bool IsActive);
