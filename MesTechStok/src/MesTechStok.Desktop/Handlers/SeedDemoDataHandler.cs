using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MesTech.Application.Commands.SeedDemoData;
using MesTech.Infrastructure.Persistence;

namespace MesTechStok.Desktop.Handlers;

/// <summary>
/// Seeds demo data using Infrastructure.DemoDataSeeder (Clean Architecture).
/// Legacy Core.AppDbContext dependency eliminated.
/// </summary>
public class SeedDemoDataHandler : IRequestHandler<SeedDemoDataCommand, SeedDemoDataResult>
{
    private readonly IServiceProvider _serviceProvider;

    public SeedDemoDataHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<SeedDemoDataResult> Handle(SeedDemoDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();

            await seeder.SeedAsync(cancellationToken);

            return new SeedDemoDataResult
            {
                IsSuccess = true,
                WasSkipped = false,
                Message = "Demo verileri yüklendi.",
            };
        }
        catch (Exception ex)
        {
            return new SeedDemoDataResult
            {
                IsSuccess = false,
                Message = ex.Message,
            };
        }
    }
}
