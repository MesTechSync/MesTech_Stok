namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Chart Of Accounts data transfer object.
/// </summary>
public sealed class ChartOfAccountsDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
    public int Level { get; set; }
}
