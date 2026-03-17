using System.Globalization;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetKdvDeclarationDraft;

namespace MesTech.Application.Features.Accounting.Queries.GetKdvReport;

/// <summary>
/// Basitlestirilmis KDV raporu handler.
/// Mevcut GetKdvDeclarationDraftQuery handler'ini cagirarak sonuclari sadece onemli
/// alanlara indirger. BeyannameSonTarih: takip eden ayin 26'si.
/// </summary>
public class GetKdvReportHandler : IRequestHandler<GetKdvReportQuery, KdvReportDto>
{
    private readonly ISender _mediator;

    public GetKdvReportHandler(ISender mediator) => _mediator = mediator;

    public async Task<KdvReportDto> Handle(
        GetKdvReportQuery request,
        CancellationToken cancellationToken)
    {
        var period = $"{request.Year:D4}-{request.Month:D2}";
        var draft = await _mediator.Send(
            new GetKdvDeclarationDraftQuery(request.TenantId, period),
            cancellationToken);

        // Beyanname son teslim tarihi: takip eden ayin 26'si
        var periodDate = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var deadlineDate = periodDate.AddMonths(1);
        var beyannameSonTarih = new DateTime(
            deadlineDate.Year, deadlineDate.Month,
            Math.Min(26, DateTime.DaysInMonth(deadlineDate.Year, deadlineDate.Month)),
            23, 59, 59, DateTimeKind.Utc);

        return new KdvReportDto
        {
            Year = request.Year,
            Month = request.Month,
            HesaplananKdv = draft.NetOutputKdv,
            IndirilecekKdv = draft.NetInputKdv,
            OdenecekKdv = draft.FinalPayableKdv,
            BeyannameSonTarih = beyannameSonTarih
        };
    }
}
