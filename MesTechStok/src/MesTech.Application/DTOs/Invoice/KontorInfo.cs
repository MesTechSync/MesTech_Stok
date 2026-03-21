namespace MesTech.Application.DTOs.Invoice;

/// <summary>
/// Kontor Info data transfer object.
/// </summary>
public record KontorInfo(
    int Remaining,
    int Total,
    DateTime? LastChecked);
