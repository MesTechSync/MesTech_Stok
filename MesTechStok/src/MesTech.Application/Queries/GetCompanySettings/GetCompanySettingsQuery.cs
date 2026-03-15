using MediatR;

namespace MesTech.Application.Queries.GetCompanySettings;

/// <summary>
/// Retrieves the current company settings (single-row pattern).
/// Used by SettingsView, WelcomeWindow, and ReportsView to display company name.
/// </summary>
public record GetCompanySettingsQuery() : IRequest<CompanySettingsDto?>;

public class CompanySettingsDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}
