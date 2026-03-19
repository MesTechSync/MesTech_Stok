using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Product.Commands.ExecuteBulkImport;

/// <summary>
/// Excel dosyasından toplu ürün içe aktarma komutu.
/// </summary>
public record ExecuteBulkImportCommand(
    Stream FileStream,
    string FileName,
    bool UpdateExisting = false,
    bool SkipErrors = true,
    PlatformType? TargetPlatform = null,
    Guid? CategoryId = null
) : IRequest<ImportResult>;
