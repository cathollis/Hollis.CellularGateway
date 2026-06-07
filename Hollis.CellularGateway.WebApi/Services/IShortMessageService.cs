using Hollis.CellularGateway.WebApi.Models.ShortMessage;

namespace Hollis.CellularGateway.WebApi.Services;

public interface IShortMessageService
{
    Task<ShortMessageModel> CreateAsync(Guid simCardId, ShortMessageModel request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShortMessageModel>> ListAsync(CancellationToken cancellationToken = default);

    Task<ShortMessageModel?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
