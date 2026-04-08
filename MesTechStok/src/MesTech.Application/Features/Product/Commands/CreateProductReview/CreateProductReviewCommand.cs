using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Product.Commands.CreateProductReview;

public record CreateProductReviewCommand(
    Guid TenantId,
    Guid ProductId,
    PlatformType Platform,
    string ExternalReviewId,
    string CustomerName,
    string Comment,
    int Rating,
    DateTime ReviewDate,
    bool IsReplied = false
) : IRequest<Guid>;
