using FluentValidation;

namespace MesTech.Application.Queries.GetSuppliers;

public sealed class GetSuppliersValidator : AbstractValidator<GetSuppliersQuery>
{
    public GetSuppliersValidator() { }
}
