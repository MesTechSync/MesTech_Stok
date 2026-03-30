namespace MesTech.Application.Features.Settings.Commands.SaveImportTemplate;

public sealed class SaveImportTemplateResult
{
    public bool IsSuccess { get; init; }
    public Guid TemplateId { get; init; }
    public string? ErrorMessage { get; init; }

    public static SaveImportTemplateResult Success(Guid templateId)
        => new() { IsSuccess = true, TemplateId = templateId };

    public static SaveImportTemplateResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}
