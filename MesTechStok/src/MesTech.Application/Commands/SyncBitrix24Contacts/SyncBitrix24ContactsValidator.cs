using FluentValidation;

namespace MesTech.Application.Commands.SyncBitrix24Contacts;

public class SyncBitrix24ContactsValidator : AbstractValidator<SyncBitrix24ContactsCommand>
{
    public SyncBitrix24ContactsValidator()
    {
        // No properties to validate — add rules as business requirements emerge
    }
}
