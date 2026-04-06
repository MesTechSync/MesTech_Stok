namespace MesTech.Application.Commands.DeleteQuotation;

public sealed record DeleteQuotationResult(bool IsSuccess, string? ErrorMessage = null);
