namespace MesTech.Application.Features.Crm.Commands.SaveCrmSettings;

public sealed class SaveCrmSettingsResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static SaveCrmSettingsResult Success()
        => new() { IsSuccess = true };

    public static SaveCrmSettingsResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}
