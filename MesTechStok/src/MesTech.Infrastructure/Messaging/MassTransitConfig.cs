using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.Messaging;

public static class MassTransitConfig
{
    public static IServiceCollection AddMesTechMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitUser = configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPass = configuration["RabbitMQ:Password"] ?? "guest";
        ushort rabbitPort = 5672;
        if (ushort.TryParse(configuration["RabbitMQ:Port"], out var parsedPort))
            rabbitPort = parsedPort;

        services.AddMassTransit(bus =>
        {
            // Consumer'lar buraya register edilecek
            // bus.AddConsumer<StockChangedConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, rabbitPort, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                // Exchange isimlendirme
                cfg.Message<StockChangedIntegrationEvent>(x =>
                    x.SetEntityName("mestech.stock.changed"));
                cfg.Message<PriceChangedIntegrationEvent>(x =>
                    x.SetEntityName("mestech.price.changed"));
                cfg.Message<OrderReceivedIntegrationEvent>(x =>
                    x.SetEntityName("mestech.order.received"));
                cfg.Message<InvoiceCreatedIntegrationEvent>(x =>
                    x.SetEntityName("mestech.invoice.created"));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
