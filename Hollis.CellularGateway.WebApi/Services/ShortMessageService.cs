using Hollis.CellularGateway.WebApi.Messaging;
using Hollis.CellularGateway.WebApi.Models.ShortMessage;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Hollis.CellularGateway.WebApi.Services;

public class ShortMessageService(DatabaseContext dbContext, IPublishEndpoint publishEndpoint) : IShortMessageService
{
    public async Task<ShortMessageModel> CreateAsync(Guid simCardId, ShortMessageModel request, CancellationToken cancellationToken)
    {
        var entity = ShortMessageModel.ToEntity(simCardId, request);

        await dbContext.ShortMessages.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publishEndpoint.Publish(new SendShortMessageEvent
        {
            ShortMessageId = entity.Id
        }, cancellationToken);

        return ShortMessageModel.FromEntity(entity);
    }

    public async Task<IReadOnlyList<ShortMessageModel>> ListAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.ShortMessages
            .AsNoTracking()
            .Include(m => m.SimCard)
            .OrderByDescending(m => m.TransmissionTime)
            .ToListAsync(cancellationToken);

        return entities.Select(ShortMessageModel.FromEntity).ToList();
    }

    public async Task<ShortMessageModel?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ShortMessages
            .AsNoTracking()
            .Include(m => m.SimCard)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        return entity is null ? null : ShortMessageModel.FromEntity(entity);
    }
}
