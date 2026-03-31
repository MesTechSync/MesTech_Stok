using MesTech.Application.DTOs;
using MesTech.Infrastructure.Persistence;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Demo tenant seed endpoints — Development only (A-M2-07 + A-M3-03).
/// POST /api/seed — re-runs DemoDataSeeder to populate DB with demo data.
/// POST /api/seed/ahmet-bey — seeds 14-step Ahmet Bey end-to-end scenario.
/// </summary>
public static class SeedEndpoints
{
    public static void Map(WebApplication app)
    {
        // Only register in Development — endpoint does not exist in production
        if (!app.Environment.IsDevelopment())
            return;

        var group = app.MapGroup("/api").WithTags("Seed").RequireRateLimiting("SystemRateLimit");

        // POST /api/seed — seed demo tenant with sample data
        group.MapPost("/seed", async (
            DemoDataSeeder seeder,
            ILogger<DemoDataSeeder> logger,
            CancellationToken ct) =>
        {
            logger.LogInformation("Manual seed requested via POST /api/seed");

            try
            {
                await seeder.SeedAsync(ct);

                return Results.Ok(new SeedResponse(
                    Success: true,
                    Message: "Demo data seeded successfully",
                    TenantName: "Demo Sirket",
                    TenantId: DemoDataSeeder.DemoTenantId.ToString(),
                    Details: new SeedDetails(
                        Products: 10,
                        Orders: 5,
                        Categories: 1,
                        Customers: 1,
                        Stores: 1
                    )
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Seed operation failed");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Seed failed");
            }
        })
        .WithName("SeedDemoData")
        .WithSummary("Demo tenant + örnek veri oluştur (sadece Development)").Produces(200).Produces(400);

        // POST /api/seed/ahmet-bey — 14-step realistic end-to-end demo scenario (A-M3-03)
        group.MapPost("/seed/ahmet-bey", async (
            AhmetBeyDemoSeeder seeder,
            ILogger<AhmetBeyDemoSeeder> logger,
            CancellationToken ct) =>
        {
            logger.LogInformation("Ahmet Bey demo seed requested via POST /api/seed/ahmet-bey");

            try
            {
                await seeder.SeedAsync(ct);

                return Results.Ok(new SeedResponse(
                    Success: true,
                    Message: "Ahmet Bey 14-step demo scenario seeded successfully",
                    TenantName: "Ahmet Ticaret A.S.",
                    TenantId: AhmetBeyDemoSeeder.AhmetBeyTenantId.ToString(),
                    Details: new SeedDetails(
                        Products: 5,
                        Orders: 3,
                        Categories: 3,
                        Customers: 1,
                        Stores: 2
                    )
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ahmet Bey seed operation failed");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Ahmet Bey seed failed");
            }
        })
        .WithName("SeedAhmetBeyDemo")
        .WithSummary("Ahmet Bey 14 adımlı gerçekçi demo senaryosu (sadece Development)").Produces(200).Produces(400);
    }

    // ── Response Records ──

    public record SeedDetails(
        int Products,
        int Orders,
        int Categories,
        int Customers,
        int Stores);

    public record SeedResponse(
        bool Success,
        string Message,
        string TenantName,
        string TenantId,
        SeedDetails Details);
}
