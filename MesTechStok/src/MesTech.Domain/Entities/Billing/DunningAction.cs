namespace MesTech.Domain.Entities.Billing;

/// <summary>Dunning islem tipi.</summary>
public enum DunningAction
{
    Warning = 0,
    Retry = 1,
    Suspend = 2,
    Cancel = 3
}
