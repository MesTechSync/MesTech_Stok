using MesTech.Domain.Enums;

namespace MesTech.Application.Queries.GetReturnRequestById;

/// <summary>
/// ReturnRequest GetById query result DTO.
/// </summary>
public sealed record GetReturnRequestByIdResult(
    Guid Id,
    Guid TenantId,
    Guid OrderId,
    string? PlatformReturnId,
    PlatformType Platform,
    ReturnStatus Status,
    ReturnReason Reason,
    string? ReasonDetail,
    string CustomerName,
    string? CustomerEmail,
    decimal RefundAmount,
    string Currency,
    string? TrackingNumber,
    bool StockRestored,
    DateTime RequestDate,
    DateTime? ApprovedAt,
    DateTime? ReceivedAt,
    DateTime? RefundedAt,
    DateTime CreatedAt);
