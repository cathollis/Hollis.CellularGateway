using Hollis.CellularGateway.WebApi.Models.SimCards;

namespace Hollis.CellularGateway.WebApi.Services;

public interface ISimCardService
{
    public Task<SimCardModel> Create(SimCardModel request, CancellationToken cancellationToken = default);

    public Task<IEnumerable<SimCardModel>> ListAsync(CancellationToken cancellationToken = default);

    public Task<SimCardModel?> GetAsync(Guid id, CancellationToken cancellationToken);
}
