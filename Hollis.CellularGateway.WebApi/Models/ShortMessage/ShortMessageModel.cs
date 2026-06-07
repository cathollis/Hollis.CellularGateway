using System.ComponentModel.DataAnnotations;

namespace Hollis.CellularGateway.WebApi.Models.ShortMessage;

public record ShortMessageModel
{
    public Guid? Id { get; init; }

    [Required]
    public required string Content { get; init; }

    public DateTimeOffset? TransmissionTime { get; init; }

    [Required]
    public required TransmissionDirectionEnum TransmissionDirection { get; init; }

    [Required]
    public required string TargetPhoneNumber { get; init; }

    public static Entities.ShortMessage ToEntity(Guid simCardId, ShortMessageModel model)
        => new()
        {
            Id = Guid.NewGuid(),
            Content = model.Content,
            TransmissionTime = model.TransmissionTime,
            TransmissionDirection = model.TransmissionDirection,
            TargetPhoneNumber = model.TargetPhoneNumber,
            SimCardId = simCardId,
        };

    public static ShortMessageModel FromEntity(Entities.ShortMessage entity)
        => new()
        {
            Id = entity.Id,
            Content = entity.Content,
            TransmissionTime = entity.TransmissionTime,
            TransmissionDirection = entity.TransmissionDirection,
            TargetPhoneNumber = entity.TargetPhoneNumber,
        };

    public enum TransmissionDirectionEnum
    {
        Inbox,
        Outbox
    }
}
