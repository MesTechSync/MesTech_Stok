namespace MesTech.Domain.Enums;

/// <summary>
/// Toplu ürün içe aktarma işlem durumu.
/// </summary>
public enum ImportStatus
{
    Pending = 0,
    Validating = 1,
    Importing = 2,
    Completed = 3,
    CompletedWithErrors = 4,
    Failed = 5,
    Cancelled = 6
}
