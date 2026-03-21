namespace MesTech.Application.DTOs;

/// <summary>
/// Invoice Result data transfer object.
/// </summary>
public class InvoiceResult
{
    public bool Success { get; set; }
    public string? GibInvoiceId { get; set; }
    public string? GibEnvelopeId { get; set; }
    public string? PdfUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
}

public class InvoiceStatusResult
{
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ResponseDate { get; set; }
}
