using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// XAdES-BES dijital imza servisi. GİB e-fatura kabul için ZORUNLU.
/// Feature flag ile aktif/pasif: XAdES.Enabled = false → orijinal XML döner.
/// Sertifika: .pfx dosyadan veya Windows Certificate Store'dan yüklenir.
/// </summary>
public class XAdESSignatureService : IDigitalSignatureService
{
    private readonly XAdESOptions _options;
    private readonly ILogger<XAdESSignatureService> _logger;

    public XAdESSignatureService(
        IOptions<XAdESOptions> options,
        ILogger<XAdESSignatureService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<byte[]> SignXmlAsync(
        byte[] xmlContent,
        SignatureType type = SignatureType.XAdES_BES,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("XAdES disabled — returning original XML");
            return xmlContent;
        }

        return await Task.Run(() =>
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(System.Text.Encoding.UTF8.GetString(xmlContent));

            var cert = LoadCertificate();
            if (cert == null)
                throw new InvalidOperationException("Mali mühür sertifikası yüklenemedi.");

            var signedXml = new SignedXml(doc)
            {
                SigningKey = cert.GetRSAPrivateKey()
            };

            // Referans — tüm doküman
            var reference = new Reference { Uri = "" };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigC14NTransform());
            signedXml.AddReference(reference);

            // KeyInfo — sertifika bilgisi
            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(cert));
            signedXml.KeyInfo = keyInfo;

            // İmzala
            signedXml.ComputeSignature();

            // XAdES-BES property'leri — SigningTime
            var signatureElement = signedXml.GetXml();
            AddXAdESProperties(doc, signatureElement, cert);

            doc.DocumentElement!.AppendChild(doc.ImportNode(signatureElement, true));

            using var ms = new MemoryStream();
            doc.Save(ms);
            return ms.ToArray();
        }, ct);
    }

    public async Task<SignatureVerificationResult> VerifySignatureAsync(
        byte[] signedXml,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(System.Text.Encoding.UTF8.GetString(signedXml));

                var signedXmlObj = new SignedXml(doc);
                var signatureNodes = doc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);

                if (signatureNodes.Count == 0)
                    return new SignatureVerificationResult(false, SignatureStatus.Unsigned, null, null, "İmza bulunamadı");

                signedXmlObj.LoadXml((XmlElement)signatureNodes[0]!);
                var isValid = signedXmlObj.CheckSignature();

                return new SignatureVerificationResult(
                    isValid,
                    isValid ? SignatureStatus.Signed : SignatureStatus.Invalid,
                    null,
                    DateTime.UtcNow,
                    isValid ? null : "İmza doğrulaması başarısız");
            }
            catch (Exception ex)
            {
                return new SignatureVerificationResult(false, SignatureStatus.Invalid, null, null, ex.Message);
            }
        }, ct);
    }

    public Task<bool> IsCertificateAvailableAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled) return Task.FromResult(false);
        var cert = LoadCertificate();
        return Task.FromResult(cert != null);
    }

    private X509Certificate2? LoadCertificate()
    {
        try
        {
            if (_options.CertificateSource == "Store" && !string.IsNullOrEmpty(_options.CertificateThumbprint))
            {
                using var store = new X509Store(StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(
                    X509FindType.FindByThumbprint, _options.CertificateThumbprint, false);
                return certs.Count > 0 ? certs[0] : null;
            }

            if (!string.IsNullOrEmpty(_options.CertificatePath) && File.Exists(_options.CertificatePath))
            {
                return new X509Certificate2(
                    _options.CertificatePath,
                    _options.CertificatePassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sertifika yüklenemedi");
        }

        return null;
    }

    private static void AddXAdESProperties(XmlDocument doc, XmlElement signatureElement, X509Certificate2 cert)
    {
        const string xadesNs = "http://uri.etsi.org/01903/v1.3.2#";
        var qualProps = doc.CreateElement("xades", "QualifyingProperties", xadesNs);
        qualProps.SetAttribute("Target", $"#signature-{Guid.NewGuid():N}");

        var signedProps = doc.CreateElement("xades", "SignedProperties", xadesNs);
        var signedSigProps = doc.CreateElement("xades", "SignedSignatureProperties", xadesNs);

        // SigningTime
        var signingTime = doc.CreateElement("xades", "SigningTime", xadesNs);
        signingTime.InnerText = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        signedSigProps.AppendChild(signingTime);

        // SigningCertificate
        var signingCert = doc.CreateElement("xades", "SigningCertificate", xadesNs);
        var certEl = doc.CreateElement("xades", "Cert", xadesNs);
        var certDigest = doc.CreateElement("xades", "CertDigest", xadesNs);
        var digestValue = doc.CreateElement("ds", "DigestValue", SignedXml.XmlDsigNamespaceUrl);
        digestValue.InnerText = Convert.ToBase64String(cert.GetCertHash());
        certDigest.AppendChild(digestValue);
        certEl.AppendChild(certDigest);
        signingCert.AppendChild(certEl);
        signedSigProps.AppendChild(signingCert);

        signedProps.AppendChild(signedSigProps);
        qualProps.AppendChild(signedProps);

        var objectEl = signatureElement.OwnerDocument!.CreateElement("ds", "Object", SignedXml.XmlDsigNamespaceUrl);
        objectEl.AppendChild(signatureElement.OwnerDocument.ImportNode(qualProps, true));
        signatureElement.AppendChild(objectEl);
    }
}

/// <summary>XAdES konfigürasyon seçenekleri.</summary>
public class XAdESOptions
{
    public bool Enabled { get; set; }
    public string CertificateSource { get; set; } = "File"; // "File" | "Store"
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
    public string? CertificateThumbprint { get; set; }
    public string DefaultSignatureType { get; set; } = "XAdES_BES";
}
