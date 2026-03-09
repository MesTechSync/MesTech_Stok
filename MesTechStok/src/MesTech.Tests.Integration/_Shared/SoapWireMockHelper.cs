namespace MesTech.Tests.Integration._Shared;

/// <summary>
/// SOAP envelope builder for WireMock stubs.
/// Sovos WSDL testleri + YurticiKargo SOAP testlerinde reuse.
/// </summary>
public static class SoapWireMockHelper
{
    private const string SoapEnvNs = "http://schemas.xmlsoap.org/soap/envelope/";

    /// <summary>
    /// Wraps arbitrary body XML inside a SOAP envelope.
    /// </summary>
    public static string BuildSoapResponse(string bodyXml)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""{SoapEnvNs}"">
  <soapenv:Header/>
  <soapenv:Body>
    {bodyXml}
  </soapenv:Body>
</soapenv:Envelope>";
    }

    /// <summary>
    /// Builds a SOAP Fault response.
    /// </summary>
    public static string BuildSoapFault(string faultCode, string faultString)
    {
        return BuildSoapResponse($@"<soapenv:Fault xmlns:soapenv=""{SoapEnvNs}"">
      <faultcode>{faultCode}</faultcode>
      <faultstring>{faultString}</faultstring>
    </soapenv:Fault>");
    }

    /// <summary>
    /// Sovos SendUBLRequest response — e-fatura gonder sonucu.
    /// </summary>
    public static string BuildSendUBLResponse(string uuid, string status = "SUCCEED")
    {
        const string ns = "http://fitcons.com/eInvoice/";
        return BuildSoapResponse($@"<SendUBLResponse xmlns=""{ns}"">
      <UUID>{uuid}</UUID>
      <Status>{status}</Status>
    </SendUBLResponse>");
    }

    /// <summary>
    /// Sovos GetEnvelopeStatusRequest response — zarf durumu.
    /// </summary>
    public static string BuildEnvelopeStatusResponse(string status, string? responseCode)
    {
        const string ns = "http://fitcons.com/eInvoice/";
        var responseCodeXml = responseCode is not null
            ? $"<ResponseCode>{responseCode}</ResponseCode>"
            : "";
        return BuildSoapResponse($@"<GetEnvelopeStatusResponse xmlns=""{ns}"">
      <Status>{status}</Status>
      {responseCodeXml}
    </GetEnvelopeStatusResponse>");
    }

    /// <summary>
    /// Sovos document data response (PDF/UBL as Base64).
    /// </summary>
    public static string BuildDocDataResponse(byte[] data)
    {
        var base64 = Convert.ToBase64String(data);
        const string ns = "http://fitcons.com/eInvoice/";
        return BuildSoapResponse($@"<GetDocDataResponse xmlns=""{ns}"">
      <DocData>{base64}</DocData>
    </GetDocDataResponse>");
    }
}
