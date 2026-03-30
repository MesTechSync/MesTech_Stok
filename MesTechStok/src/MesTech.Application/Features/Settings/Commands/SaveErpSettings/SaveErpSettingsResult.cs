namespace MesTech.Application.Features.Settings.Commands.SaveErpSettings;

public sealed class SaveErpSettingsResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static SaveErpSettingsResult Success()
        => new() { IsSuccess = true };

    public static SaveErpSettingsResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}
