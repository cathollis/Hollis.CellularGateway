using Hollis.CellularGateway.WebApi.Models.SimCards;
using Microsoft.EntityFrameworkCore;

namespace Hollis.CellularGateway.WebApi.Services;

public class SimCardService(DatabaseContext dbContext) : ISimCardService
{
    public async Task<SimCardModel> Create(SimCardModel request, CancellationToken cancellationToken)
    {
        var entity = SimCardModel.ToEntity(request);

        await dbContext.SimCards
            .AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return SimCardModel.FromEntity(entity);
    }

    public async Task<IEnumerable<SimCardModel>> ListAsync(CancellationToken cancellationToken)
    {
        var list = await dbContext.SimCards
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return list.Select(SimCardModel.FromEntity);
    }

    public async Task<SimCardModel?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.SimCards
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity != null ? SimCardModel.FromEntity(entity) : null;
    }
}
