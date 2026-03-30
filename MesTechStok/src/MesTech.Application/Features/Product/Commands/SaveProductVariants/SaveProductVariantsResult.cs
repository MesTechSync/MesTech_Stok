namespace MesTech.Application.Features.Product.Commands.SaveProductVariants;

public sealed class SaveProductVariantsResult
{
    public bool IsSuccess { get; init; }
    public int SavedCount { get; init; }
    public string? ErrorMessage { get; init; }

    public static SaveProductVariantsResult Success(int savedCount)
        => new() { IsSuccess = true, SavedCount = savedCount };

    public static SaveProductVariantsResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}
