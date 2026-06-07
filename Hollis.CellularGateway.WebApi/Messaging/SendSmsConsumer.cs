using Hollis.CellularGateway.AtClient;
using Hollis.CellularGateway.WebApi.Models.ShortMessage;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Hollis.CellularGateway.WebApi.Messaging;

/// <summary>
/// Consumes <see cref="SendShortMessageEvent"/> messages: reads the ShortMessage from the database
/// and sends it via the <see cref="AtClient"/> modem.
/// </summary>
public class SendSmsConsumer(
    IDbContextFactory<DatabaseContext> dbContextFactory,
    AtClient.AtClient atClient,
    ILogger<SendSmsConsumer> logger) : IConsumer<SendShortMessageEvent>
{
    public async Task Consume(ConsumeContext<SendShortMessageEvent> context)
    {
        var id = context.Message.ShortMessageId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(context.CancellationToken);

        var message = await dbContext.ShortMessages
            .Include(m => m.SimCard)
            .FirstOrDefaultAsync(m => m.Id == id, context.CancellationToken);

        if (message is null)
        {
            logger.LogWarning("ShortMessage {Id} not found in database", id);
            return;
        }

        if (message.TransmissionDirection != ShortMessageModel.TransmissionDirectionEnum.Outbox)
        {
            logger.LogWarning("ShortMessage {Id} is not an outbox message, skipping send", id);
            return;
        }

        logger.LogInformation("Sending SMS {Id} to {PhoneNumber}", id, message.TargetPhoneNumber);

        try
        {
            await atClient.SendSmsAsync(message.TargetPhoneNumber, message.Content, context.CancellationToken);

            message.TransmissionTime = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation("SMS {Id} sent successfully", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SMS {Id}", id);
            throw;
        }
    }
}
