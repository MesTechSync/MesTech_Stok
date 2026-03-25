using Mapster;
using MesTech.Application.DTOs.EInvoice;
using MesTech.Domain.Entities.EInvoice;

namespace MesTech.Application.Mapping;

public sealed class EInvoiceMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<EInvoiceDocument, EInvoiceDto>();
        config.NewConfig<EInvoiceLine, EInvoiceLineDto>();
    }
}
