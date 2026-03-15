using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Entities.Documents;
using MesTech.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Seed;

public static class CrmSeedData
{
    private static readonly Guid DefaultTenantId = new("00000000-0000-0000-0000-000000000001");

    public static async Task SeedDefaultPipelineAsync(AppDbContext context)
    {
        if (await context.Set<Pipeline>().AnyAsync()) return;

        var pipeline = Pipeline.Create(DefaultTenantId, "Satış Hunisi", true, 1);
        await context.Set<Pipeline>().AddAsync(pipeline);

        var stages = new[]
        {
            PipelineStage.Create(DefaultTenantId, pipeline.Id, "İlk İletişim",  1, 10m,  StageType.Normal, "#3B82F6"),
            PipelineStage.Create(DefaultTenantId, pipeline.Id, "Teklif Verildi",2, 40m,  StageType.Normal, "#F59E0B"),
            PipelineStage.Create(DefaultTenantId, pipeline.Id, "Müzakere",      3, 70m,  StageType.Normal, "#8B5CF6"),
            PipelineStage.Create(DefaultTenantId, pipeline.Id, "Kazanıldı ✓",  4, 100m, StageType.Won,    "#10B981"),
            PipelineStage.Create(DefaultTenantId, pipeline.Id, "Kaybedildi ✗", 5, 0m,   StageType.Lost,   "#EF4444"),
        };
        await context.Set<PipelineStage>().AddRangeAsync(stages);

        if (!await context.Set<DocumentFolder>().AnyAsync())
        {
            var systemFolders = new[]
            {
                DocumentFolder.Create(DefaultTenantId, "Siparişler",      position: 1, isSystem: true),
                DocumentFolder.Create(DefaultTenantId, "Faturalar",       position: 2, isSystem: true),
                DocumentFolder.Create(DefaultTenantId, "Ürün Görselleri", position: 3, isSystem: true),
                DocumentFolder.Create(DefaultTenantId, "Sözleşmeler",     position: 4, isSystem: true),
                DocumentFolder.Create(DefaultTenantId, "Kargo Belgeleri", position: 5, isSystem: true),
            };
            await context.Set<DocumentFolder>().AddRangeAsync(systemFolders);
        }

        await context.SaveChangesAsync();
    }
}
