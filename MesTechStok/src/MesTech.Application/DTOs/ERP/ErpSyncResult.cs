namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// Dalga 9: ERP sync operation result — returned by SyncEInvoiceAsync and future ERP sync methods.
/// Success: whether the record was accepted by the ERP.
/// ErpRef: the ERP-side record ID on success.
/// ErrorMessage: error detail on failure.
/// </summary>
public record ErpSyncResult(bool Success, string? ErpRef, string? ErrorMessage);
