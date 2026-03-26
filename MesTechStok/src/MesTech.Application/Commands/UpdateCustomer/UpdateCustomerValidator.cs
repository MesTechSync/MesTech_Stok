using FluentValidation;

namespace MesTech.Application.Commands.UpdateCustomer;

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => x.Email != null);
        RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone != null);
        RuleFor(x => x.ContactPerson).MaximumLength(200).When(x => x.ContactPerson != null);
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City != null);
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => x.TaxNumber != null);
        RuleFor(x => x.TaxOffice).MaximumLength(200).When(x => x.TaxOffice != null);
    }
}
