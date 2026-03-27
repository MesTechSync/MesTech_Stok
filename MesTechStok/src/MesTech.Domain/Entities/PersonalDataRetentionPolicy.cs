using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// KVKK Madde 7 + ASVS V8 — kisisel veri saklama suresi politikasi.
/// Her entity tipi icin saklama suresi (gun), anonimlesme stratejisi tanimlanir.
/// DataRetentionJob bu tabloyu okuyarak policy-driven calisir.
/// </summary>
public sealed class PersonalDataRetentionPolicy : BaseEntity
{
    public string EntityTypeName { get; private set; } = string.Empty;
    public int RetentionDays { get; private set; }
    public string AnonymizationStrategy { get; private set; } = "Hash";
    public string? FieldsToAnonymize { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }

    private PersonalDataRetentionPolicy() { }

    public static PersonalDataRetentionPolicy Create(
        string entityTypeName,
        int retentionDays,
        string anonymizationStrategy = "Hash",
        string? fieldsToAnonymize = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityTypeName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retentionDays);

        return new PersonalDataRetentionPolicy
        {
            Id = Guid.NewGuid(),
            EntityTypeName = entityTypeName,
            RetentionDays = retentionDays,
            AnonymizationStrategy = anonymizationStrategy,
            FieldsToAnonymize = fieldsToAnonymize,
            IsActive = true,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateRetention(int retentionDays, string? notes = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retentionDays);
        RetentionDays = retentionDays;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
