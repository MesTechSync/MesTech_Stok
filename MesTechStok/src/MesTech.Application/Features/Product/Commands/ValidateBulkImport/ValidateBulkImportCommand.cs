using MediatR;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Product.Commands.ValidateBulkImport;

/// <summary>
/// Excel dosyasını import öncesi doğrulama komutu.
/// </summary>
public record ValidateBulkImportCommand(
    Stream FileStream,
    string FileName
) : IRequest<ImportValidationResult>;
