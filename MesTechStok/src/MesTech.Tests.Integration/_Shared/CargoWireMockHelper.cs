namespace MesTech.Tests.Integration._Shared;

/// <summary>
/// WireMock response builder for cargo adapter contract tests.
/// REST responses (Aras, Surat): JSON format.
/// SOAP responses (Yurtici): XML wrapped via SoapWireMockHelper.
/// </summary>
public static class CargoWireMockHelper
{
    // ══════════════════════════════════════
    // REST (JSON) — Aras Kargo, Surat Kargo
    // ══════════════════════════════════════

    /// <summary>
    /// Builds a JSON response for CreateShipment success.
    /// </summary>
    public static string BuildCreateShipmentResponse(
        string trackingNumber,
        string shipmentId,
        bool success = true)
    {
        return $$"""
        {
            "success": {{success.ToString().ToLowerInvariant()}},
            "trackingNumber": "{{trackingNumber}}",
            "shipmentId": "{{shipmentId}}"
        }
        """;
    }

    /// <summary>
    /// Builds a JSON response for TrackShipment with status and checkpoints.
    /// </summary>
    public static string BuildTrackingResponse(
        string trackingNumber,
        string status,
        (string timestamp, string location, string description, string status)[]? checkpoints = null)
    {
        var eventsJson = "[]";
        if (checkpoints is { Length: > 0 })
        {
            var items = checkpoints.Select(cp =>
                $$"""
                {
                    "timestamp": "{{cp.timestamp}}",
                    "location": "{{cp.location}}",
                    "description": "{{cp.description}}",
                    "status": "{{cp.status}}"
                }
                """);
            eventsJson = $"[{string.Join(",", items)}]";
        }

        return $$"""
        {
            "trackingNumber": "{{trackingNumber}}",
            "status": "{{status}}",
            "estimatedDelivery": "2026-03-15T18:00:00Z",
            "events": {{eventsJson}}
        }
        """;
    }

    /// <summary>
    /// Builds a JSON response for CancelShipment.
    /// </summary>
    public static string BuildCancelResponse(bool success, string? message = null)
    {
        var msgPart = message is not null ? $@", ""message"": ""{message}""" : "";
        return $$"""
        {
            "success": {{success.ToString().ToLowerInvariant()}}{{msgPart}}
        }
        """;
    }

    /// <summary>
    /// Builds a JSON response for GetShipmentLabel (base64 PDF).
    /// </summary>
    public static string BuildLabelResponse(string base64Pdf)
    {
        return $$"""
        {
            "labelData": "{{base64Pdf}}",
            "format": "PDF"
        }
        """;
    }

    /// <summary>
    /// Builds a JSON response for health check.
    /// </summary>
    public static string BuildHealthResponse(bool healthy = true)
    {
        return $$"""
        {
            "status": "{{(healthy ? "healthy" : "unhealthy")}}",
            "timestamp": "2026-03-11T10:00:00Z"
        }
        """;
    }

    /// <summary>
    /// Builds a JSON error response.
    /// </summary>
    public static string BuildErrorResponse(int statusCode, string message)
    {
        return $$"""
        {
            "error": {
                "code": {{statusCode}},
                "message": "{{message}}"
            }
        }
        """;
    }

    // ══════════════════════════════════════
    // SOAP (Yurtici Kargo) — delegates to SoapWireMockHelper
    // ══════════════════════════════════════

    private const string YkNs = "http://yurticikargo.com/";

    /// <summary>
    /// Builds a SOAP envelope for Yurtici Kargo CreateShipment response.
    /// </summary>
    public static string BuildSoapCreateShipmentResponse(string trackingNumber, string jobId)
    {
        var body = $@"<createShipmentResponse xmlns=""{YkNs}"">
      <return>
        <cargoKey>{trackingNumber}</cargoKey>
        <invDocId>INV-{trackingNumber}</invDocId>
        <jobId>{jobId}</jobId>
        <result>OK</result>
      </return>
    </createShipmentResponse>";

        return SoapWireMockHelper.BuildSoapResponse(body);
    }

    /// <summary>
    /// Builds a SOAP envelope for Yurtici Kargo tracking response.
    /// </summary>
    public static string BuildSoapTrackingResponse(string trackingNumber, string status, string description)
    {
        var body = $@"<queryShipmentResponse xmlns=""{YkNs}"">
      <return>
        <operationCode>{status}</operationCode>
        <estimatedArrivalDate>2026-03-15</estimatedArrivalDate>
        <ShipmentEventVO>
          <eventDate>2026-03-11T10:00:00</eventDate>
          <unitName>Merkez Depo</unitName>
          <eventName>{description}</eventName>
          <operationCode>{status}</operationCode>
        </ShipmentEventVO>
      </return>
    </queryShipmentResponse>";

        return SoapWireMockHelper.BuildSoapResponse(body);
    }

    /// <summary>
    /// Builds a SOAP envelope for Yurtici Kargo cancel response.
    /// Yurtici does not support cancellation, but this can be used for negative tests.
    /// </summary>
    public static string BuildSoapCancelResponse(bool success)
    {
        var body = $@"<cancelShipmentResponse xmlns=""{YkNs}"">
      <return>
        <success>{success.ToString().ToLowerInvariant()}</success>
      </return>
    </cancelShipmentResponse>";

        return SoapWireMockHelper.BuildSoapResponse(body);
    }

    /// <summary>
    /// Builds a SOAP envelope for Yurtici Kargo label response.
    /// </summary>
    public static string BuildSoapLabelResponse(string base64Pdf)
    {
        var body = $@"<createShipmentLabelResponse xmlns=""{YkNs}"">
      <return>
        <labelData>{base64Pdf}</labelData>
        <labelFormat>PDF</labelFormat>
      </return>
    </createShipmentLabelResponse>";

        return SoapWireMockHelper.BuildSoapResponse(body);
    }

    /// <summary>
    /// Builds a SOAP Fault for Yurtici Kargo.
    /// </summary>
    public static string BuildSoapFault(string faultCode, string faultString)
    {
        return SoapWireMockHelper.BuildSoapFault(faultCode, faultString);
    }
}
