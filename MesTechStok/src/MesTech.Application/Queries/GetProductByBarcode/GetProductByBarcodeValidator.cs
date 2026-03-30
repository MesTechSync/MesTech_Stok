using FluentValidation;

namespace MesTech.Application.Queries.GetProductByBarcode;

public sealed class GetProductByBarcodeValidator : AbstractValidator<GetProductByBarcodeQuery>
{
    public GetProductByBarcodeValidator()
    {
        RuleFor(x => x.Barcode).NotEmpty().MaximumLength(200);
    }
}
