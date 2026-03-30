namespace MesTech.Application.Features.Settings.Commands.SaveFulfillmentSettings;

public sealed class SaveFulfillmentSettingsResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static SaveFulfillmentSettingsResult Success()
        => new() { IsSuccess = true };

    public static SaveFulfillmentSettingsResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}
