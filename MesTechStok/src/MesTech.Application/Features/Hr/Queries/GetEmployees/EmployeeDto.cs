namespace MesTech.Application.Features.Hr.Queries.GetEmployees;

public class EmployeeDto
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string WorkEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public Guid? DepartmentId { get; set; }
}
