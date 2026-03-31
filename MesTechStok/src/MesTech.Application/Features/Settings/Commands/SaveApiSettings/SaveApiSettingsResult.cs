namespace MesTech.Application.Features.Settings.Commands.SaveApiSettings;

public sealed class SaveApiSettingsResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static SaveApiSettingsResult Success()
        => new() { IsSuccess = true };

    public static SaveApiSettingsResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}
