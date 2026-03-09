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

    // ── N11 SOAP Helpers ──
    // N11 Partner API: https://api.n11.com/ws
    // Namespace: urn:partnerService

    private const string N11Ns = "urn:partnerService";

    /// <summary>
    /// N11 GetProductListBySellerCode response.
    /// </summary>
    public static string BuildN11GetProductListResponse(int count)
    {
        var items = Enumerable.Range(1, count).Select(i =>
            $@"<products>
          <id>{i}000</id>
          <productSellerCode>SKU-{i:D4}</productSellerCode>
          <title>Test Product {i}</title>
          <stockItems><stockItem><quantity>10</quantity><sellerStockCode>WH-01</sellerStockCode></stockItem></stockItems>
        </products>").Aggregate("", (acc, s) => acc + s);

        return BuildSoapResponse($@"<GetProductListBySellerCodeResponse xmlns=""{N11Ns}"">
      <result><status>success</status></result>
      <productList>
        {items}
        <totalCount>{count}</totalCount>
        <currentPage>0</currentPage>
        <pageSize>50</pageSize>
      </productList>
    </GetProductListBySellerCodeResponse>");
    }

    /// <summary>
    /// N11 SaveProduct response (create / update).
    /// </summary>
    public static string BuildN11SaveProductResponse(string productId, string status = "success")
    {
        return BuildSoapResponse($@"<SaveProductResponse xmlns=""{N11Ns}"">
      <result><status>{status}</status></result>
      <product><id>{productId}</id></product>
    </SaveProductResponse>");
    }

    /// <summary>
    /// N11 UpdateProductBasic response (stock / price patch).
    /// </summary>
    public static string BuildN11UpdateProductResponse(bool success)
    {
        var status = success ? "success" : "failure";
        return BuildSoapResponse($@"<UpdateProductBasicResponse xmlns=""{N11Ns}"">
      <result><status>{status}</status></result>
    </UpdateProductBasicResponse>");
    }

    /// <summary>
    /// N11 GetOrderList response.
    /// </summary>
    public static string BuildN11GetOrderListResponse(int count)
    {
        var orders = Enumerable.Range(1, count).Select(i =>
            $@"<orderList>
          <id>{i}00000</id>
          <status>New</status>
          <createDate>2026-03-09 10:00:00</createDate>
        </orderList>").Aggregate("", (acc, s) => acc + s);

        return BuildSoapResponse($@"<GetOrderListResponse xmlns=""{N11Ns}"">
      <result><status>success</status></result>
      {orders}
      <pagingData><totalCount>{count}</totalCount></pagingData>
    </GetOrderListResponse>");
    }

    /// <summary>
    /// N11 GetCategoryList response.
    /// </summary>
    public static string BuildN11GetCategoryListResponse(int count)
    {
        var cats = Enumerable.Range(1, count).Select(i =>
            $@"<categories>
          <id>{i}</id>
          <name>Kategori {i}</name>
        </categories>").Aggregate("", (acc, s) => acc + s);

        return BuildSoapResponse($@"<GetCategoryListResponse xmlns=""{N11Ns}"">
      <result><status>success</status></result>
      {cats}
    </GetCategoryListResponse>");
    }
}
