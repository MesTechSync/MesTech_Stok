using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.CreateSalaryRecord;

public record CreateSalaryRecordCommand(
    Guid TenantId,
    string EmployeeName,
    decimal GrossSalary,
    decimal SGKEmployer,
    decimal SGKEmployee,
    decimal IncomeTax,
    decimal StampTax,
    int Year,
    int Month,
    Guid? EmployeeId = null,
    string? Notes = null
) : IRequest<Guid>;
