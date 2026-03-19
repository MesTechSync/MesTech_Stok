using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// XAdES dijital imza servisi. UBL-TR XML'i GİB uyumlu şekilde imzalar.
/// Feature flag ile aktif/pasif yapılabilir.
/// </summary>
public interface IDigitalSignatureService
{
    /// <summary>UBL-TR XML'i XAdES-BES ile imzalar.</summary>
    Task<byte[]> SignXmlAsync(
        byte[] xmlContent,
        SignatureType type = SignatureType.XAdES_BES,
        CancellationToken ct = default);

    /// <summary>İmzalı XML'in geçerliliğini doğrular.</summary>
    Task<SignatureVerificationResult> VerifySignatureAsync(
        byte[] signedXml,
        CancellationToken ct = default);

    /// <summary>Mali mühür sertifikasının mevcut olup olmadığını kontrol eder.</summary>
    Task<bool> IsCertificateAvailableAsync(CancellationToken ct = default);
}

public record SignatureVerificationResult(
    bool IsValid,
    SignatureStatus Status,
    string? SignerName,
    DateTime? SigningTime,
    string? ErrorMessage);
