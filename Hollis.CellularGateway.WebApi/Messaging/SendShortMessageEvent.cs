namespace Hollis.CellularGateway.WebApi.Messaging;

public record SendShortMessageEvent
{
    public Guid ShortMessageId { get; init; }
}
