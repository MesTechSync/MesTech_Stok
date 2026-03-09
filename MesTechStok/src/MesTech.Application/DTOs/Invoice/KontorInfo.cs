namespace MesTech.Application.DTOs.Invoice;

public record KontorInfo(
    int Remaining,
    int Total,
    DateTime? LastChecked);
