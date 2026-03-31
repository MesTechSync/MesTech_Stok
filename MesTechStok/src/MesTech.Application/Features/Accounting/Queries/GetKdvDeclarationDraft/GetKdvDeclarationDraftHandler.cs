#pragma warning disable MA0051 // Method is too long — KDV declaration handler is a single cohesive operation
using System.Globalization;
using System.Text;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetKdvDeclarationDraft;

/// <summary>
/// KDV beyanname taslak hesaplama handler.
/// Aylik KDV1/KDV2 draft: Hesaplanan KDV - Indirilecek KDV = Odenecek KDV.
///
/// Veri kaynaklari:
///   - TaxRecord tablosu: donem bazinda KDV, KDV-Alis, KDV-Iade, KDV-Tevkifat kayitlari
///   - CommissionRecord tablosu: platform komisyon kayitlari (komisyon KDV icin)
///   - TaxWithholding tablosu: KDV tevkifat kayitlari
///
/// Turk vergi mevzuati:
///   Output VAT (Hesaplanan KDV, 391) = Satis KDV - Iade KDV duzeltme
///   Input VAT (Indirilecek KDV, 191) = Alis KDV + Komisyon KDV
///   Odenecek KDV = Output - Input - Tevkifat - Devreden
/// </summary>
public sealed class GetKdvDeclarationDraftHandler
    : IRequestHandler<GetKdvDeclarationDraftQuery, KdvDeclarationDraftDto>
{
    private readonly ITaxRecordRepository _taxRecordRepo;
    private readonly ICommissionRecordRepository _commissionRepo;
    private readonly ITaxWithholdingRepository _withholdingRepo;

    private static readonly CultureInfo TrCulture = new("tr-TR");

    public GetKdvDeclarationDraftHandler(
        ITaxRecordRepository taxRecordRepo,
        ICommissionRecordRepository commissionRepo,
        ITaxWithholdingRepository withholdingRepo)
    {
        _taxRecordRepo = taxRecordRepo;
        _commissionRepo = commissionRepo;
        _withholdingRepo = withholdingRepo;
    }

    public async Task<KdvDeclarationDraftDto> Handle(
        GetKdvDeclarationDraftQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = request.TenantId;
        var period = request.Period;

        // Parse period to date range (yyyy-MM)
        var periodDate = DateTime.ParseExact(period, "yyyy-MM", CultureInfo.InvariantCulture);
        var periodStart = new DateTime(periodDate.Year, periodDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        // 1. Fetch tax records for the period
        var taxRecords = await _taxRecordRepo.GetByPeriodAsync(tenantId, period, cancellationToken).ConfigureAwait(false);

        // 2. Calculate Output KDV (Hesaplanan)
        var salesKdv = taxRecords
            .Where(r => string.Equals(r.TaxType, "KDV", StringComparison.Ordinal)
                     || string.Equals(r.TaxType, "KDV-Satis", StringComparison.Ordinal))
            .Sum(r => r.TaxAmount);

        var returnKdvAdjustment = taxRecords
            .Where(r => string.Equals(r.TaxType, "KDV-Iade", StringComparison.Ordinal))
            .Sum(r => r.TaxAmount);

        var netOutputKdv = salesKdv - returnKdvAdjustment;

        // 3. Calculate Input KDV (Indirilecek)
        var purchaseKdv = taxRecords
            .Where(r => string.Equals(r.TaxType, "KDV-Alis", StringComparison.Ordinal))
            .Sum(r => r.TaxAmount);

        // Commission KDV — from commission records in the period
        var totalCommission = await _commissionRepo
            .GetTotalCommissionAsync(tenantId, periodStart, periodEnd, cancellationToken);
        // Standard KDV rate for commission invoices: %20 (2024+ rate)
        var commissionKdv = Math.Round(totalCommission * 0.20m, 2);

        var netInputKdv = purchaseKdv + commissionKdv;

        // 4. Withholding KDV (Tevkifat) — from tax records
        var withholdingKdv = taxRecords
            .Where(r => string.Equals(r.TaxType, "KDV-Tevkifat", StringComparison.Ordinal))
            .Sum(r => r.TaxAmount);

        // 5. Carry-forward KDV (Devreden) from previous period
        var carryForwardKdv = taxRecords
            .Where(r => string.Equals(r.TaxType, "KDV-Devreden", StringComparison.Ordinal))
            .Sum(r => r.TaxAmount);

        // 6. Calculate payable
        var payableKdv = netOutputKdv - netInputKdv - withholdingKdv;
        var finalPayable = payableKdv - carryForwardKdv;

        // 7. Build report text
        var reportText = BuildReportText(
            period, periodDate,
            salesKdv, returnKdvAdjustment, netOutputKdv,
            purchaseKdv, commissionKdv, netInputKdv,
            withholdingKdv, carryForwardKdv,
            payableKdv, finalPayable);

        return new KdvDeclarationDraftDto
        {
            Period = period,
            SalesKdv = salesKdv,
            ReturnKdvAdjustment = returnKdvAdjustment,
            NetOutputKdv = netOutputKdv,
            PurchaseKdv = purchaseKdv,
            CommissionKdv = commissionKdv,
            NetInputKdv = netInputKdv,
            WithholdingKdv = withholdingKdv,
            PayableKdv = payableKdv,
            CarryForwardKdv = carryForwardKdv,
            FinalPayableKdv = finalPayable,
            ReportText = reportText
        };
    }

    private static string BuildReportText(
        string period,
        DateTime periodDate,
        decimal salesKdv, decimal returnKdvAdj, decimal netOutputKdv,
        decimal purchaseKdv, decimal commissionKdv, decimal netInputKdv,
        decimal withholdingKdv, decimal carryForwardKdv,
        decimal payableKdv, decimal finalPayable)
    {
        var monthName = periodDate.ToString("MMMM yyyy", TrCulture);
        var sb = new StringBuilder();

        sb.AppendLine(TrCulture, $"KDV BEYANNAME TASLAK — {monthName.ToUpper(TrCulture)}");
        sb.AppendLine(TrCulture, $"Donem: {period}");
        sb.AppendLine(new string('\u2500', 50));
        sb.AppendLine();

        sb.AppendLine("A) HESAPLANAN KDV (Output)");
        sb.AppendLine(TrCulture, $"   Satis KDV (391):             {salesKdv,14:N2} TL");
        sb.AppendLine(TrCulture, $"   Iade KDV Duzeltme:           {(-returnKdvAdj),14:N2} TL");
        sb.AppendLine(TrCulture, $"   Net Hesaplanan KDV:          {netOutputKdv,14:N2} TL");
        sb.AppendLine();

        sb.AppendLine("B) INDIRILECEK KDV (Input)");
        sb.AppendLine(TrCulture, $"   Alis KDV (191):              {purchaseKdv,14:N2} TL");
        sb.AppendLine(TrCulture, $"   Komisyon KDV:                {commissionKdv,14:N2} TL");
        sb.AppendLine(TrCulture, $"   Net Indirilecek KDV:         {netInputKdv,14:N2} TL");
        sb.AppendLine();

        if (withholdingKdv > 0)
        {
            sb.AppendLine("C) KDV TEVKIFAT (9015)");
            sb.AppendLine(TrCulture, $"   Tevkifat Tutari:             {withholdingKdv,14:N2} TL");
            sb.AppendLine();
        }

        sb.AppendLine(new string('\u2500', 50));
        sb.AppendLine(TrCulture, $"   ODENECEK KDV (A-B-C):        {payableKdv,14:N2} TL");

        if (carryForwardKdv > 0)
        {
            sb.AppendLine(TrCulture, $"   Devreden KDV (190):          {(-carryForwardKdv),14:N2} TL");
        }

        sb.AppendLine(new string('\u2550', 50));
        sb.AppendLine(TrCulture, $"   DONEM SONU NET:               {finalPayable,14:N2} TL");

        if (finalPayable < 0)
        {
            sb.AppendLine();
            sb.AppendLine($"   * Negatif tutar sonraki doneme KDV olarak devredecektir (190 hesabi).");
        }

        return sb.ToString();
    }
}
