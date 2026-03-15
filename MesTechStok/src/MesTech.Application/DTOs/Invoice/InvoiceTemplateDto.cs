using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Invoice;

public record InvoiceTemplateDto(
    IReadOnlyList<byte>? LogoImage,
    IReadOnlyList<byte>? SignatureImage,
    string? PhoneNumber,
    string? Email,
    string? TicaretSicilNo,
    bool ShowKargoBarkodu,
    bool ShowFaturaTutariYaziyla,
    KdvRate DefaultKdv);
