namespace MesTech.Domain.Enums;

/// <summary>
/// İade nedenleri — platform-agnostik.
/// </summary>
public enum ReturnReason
{
    None = 0,
    DefectiveProduct = 1,
    WrongProduct = 2,
    WrongSize = 3,
    WrongColor = 4,
    NotAsDescribed = 5,
    DamagedInShipping = 6,
    LateDelivery = 7,
    CustomerRegret = 8,
    MissingParts = 9,
    QualityIssue = 10,
    Other = 99
}
