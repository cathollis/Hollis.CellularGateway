using System.ComponentModel.DataAnnotations;
using Hollis.CellularGateway.WebApi.Entities;

namespace Hollis.CellularGateway.WebApi.Models.SimCards;

public record SimCardModel
{
    public Guid? Id { get; init; }

    [Required]
    public required string DisplayName { get; init; }

    public string? Description { get; init; }

    [Required]
    public required string InternationalMobileSubscriberId { get; init; }

    public static SimCard ToEntity(SimCardModel request)
        => new()
        {
            Id = Guid.NewGuid(),
            DisplayName = request.DisplayName,
            Description = request.Description,
            InternationalMobileSubscriberId = request.InternationalMobileSubscriberId,
        };

    public static SimCardModel FromEntity(SimCard entity)
        => new()
        {
            Id = entity.Id,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            InternationalMobileSubscriberId = entity.InternationalMobileSubscriberId,
        };
}
