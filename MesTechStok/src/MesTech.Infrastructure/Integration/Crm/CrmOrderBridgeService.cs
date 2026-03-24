using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Infrastructure.Integration.Crm;

/// <summary>
/// CRM ↔ Order cift yonlu kopru.
/// Deal kazanilinca Order baglantisi kurulur; yeni pazaryeri siparisi gelince Lead olusturur.
/// </summary>
public class CrmOrderBridgeService : ICrmOrderBridgeService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CrmOrderBridgeService> _logger;

    public CrmOrderBridgeService(
        AppDbContext context,
        IUnitOfWork uow,
        ILogger<CrmOrderBridgeService> logger)
    {
        _context = context;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Guid> CreateOrderFromDealAsync(Guid dealId, CancellationToken ct = default)
    {
        var deal = await _context.Set<Deal>()
            .Include(d => d.Contact)
            .FirstOrDefaultAsync(d => d.Id == dealId, ct)
            .ConfigureAwait(false);
        if (deal is null)
            throw new InvalidOperationException($"Deal {dealId} not found.");

        if (deal.Status != DealStatus.Won)
            throw new InvalidOperationException("Only won deals can create an order.");

        // Yeni Order ID olustur — Deal.OrderId'yi guncelle
        var orderId = Guid.NewGuid();
        deal.LinkOrder(orderId);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "CrmOrderBridge: Deal {DealId} → Order {OrderId} linked",
            dealId, orderId);

        return orderId;
    }

    public async Task<Guid?> CreateLeadFromOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _context.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            .ConfigureAwait(false);

        if (order is null)
        {
            _logger.LogWarning("CrmOrderBridge: Order {OrderId} not found", orderId);
            return null;
        }

        var customerName = order.CustomerName ?? "Bilinmeyen Musteri";

        // Ayni musteri adi ile aktif lead var mi?
        var existingLead = await _context.Set<Lead>()
            .AnyAsync(l => l.TenantId == order.TenantId
                        && l.FullName == customerName
                        && l.Status != LeadStatus.Lost, ct)
            .ConfigureAwait(false);

        if (existingLead)
        {
            _logger.LogInformation(
                "CrmOrderBridge: Lead already exists for customer {Name}", customerName);
            return null;
        }

        var lead = Lead.Create(
            tenantId: order.TenantId,
            fullName: customerName,
            source: LeadSource.MarketplaceInquiry,
            email: order.CustomerEmail,
            phone: null);

        await _context.Set<Lead>().AddAsync(lead, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "CrmOrderBridge: Order {OrderId} → Lead {LeadId} created ({Name})",
            orderId, lead.Id, customerName);

        return lead.Id;
    }
}
