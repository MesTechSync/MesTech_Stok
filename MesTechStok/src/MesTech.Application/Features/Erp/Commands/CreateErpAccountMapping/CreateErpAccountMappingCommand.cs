using MediatR;

namespace MesTech.Application.Features.Erp.Commands.CreateErpAccountMapping;

/// <summary>
/// MesTech ↔ ERP hesap eslestirme komutu.
/// G420: Avalonia "Esle" butonu icin backend command.
/// </summary>
public record CreateErpAccountMappingCommand(
    Guid TenantId,
    string MesTechCode,
    string MesTechName,
    string MesTechType,
    string ErpCode,
    string ErpName) : IRequest<CreateErpAccountMappingResult>;

public sealed class CreateErpAccountMappingResult
{
    public bool IsSuccess { get; init; }
    public Guid MappingId { get; init; }
    public string? ErrorMessage { get; init; }

    public static CreateErpAccountMappingResult Success(Guid id) => new() { IsSuccess = true, MappingId = id };
    public static CreateErpAccountMappingResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
