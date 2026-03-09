using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Invoice;

public record InvoiceTemplateDto(
    byte[]? LogoImage,
    byte[]? SignatureImage,
    string? PhoneNumber,
    string? Email,
    string? TicaretSicilNo,
    bool ShowKargoBarkodu,
    bool ShowFaturaTutariYaziyla,
    KdvRate DefaultKdv);
