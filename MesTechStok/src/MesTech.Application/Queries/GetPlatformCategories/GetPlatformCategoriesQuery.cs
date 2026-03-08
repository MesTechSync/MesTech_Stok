using MesTech.Application.DTOs;
using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Queries.GetPlatformCategories;

public record GetPlatformCategoriesQuery(PlatformType Platform) : IRequest<List<PlatformCategoryDto>>;
