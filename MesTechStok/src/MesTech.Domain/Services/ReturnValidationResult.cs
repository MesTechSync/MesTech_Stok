using MesTech.Domain.Enums;

namespace MesTech.Domain.Services;

/// <summary>
/// İade doğrulama sonucu.
/// </summary>
public record ReturnValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public PlatformReturnPolicy? Policy { get; init; }

    public static ReturnValidationResult Success(PlatformReturnPolicy policy) =>
        new() { IsValid = true, Policy = policy };

    public static ReturnValidationResult Fail(string errorMessage) =>
        new() { IsValid = false, ErrorMessage = errorMessage };
}
