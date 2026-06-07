using Hollis.CellularGateway.WebApi.Consts;
using Hollis.CellularGateway.WebApi.Models.SimCards;
using Hollis.CellularGateway.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace Hollis.CellularGateway.WebApi.Controllers;

[Authorize]
[ApiController]
[Route(SimCardConst.Route)]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public class SimCardsController(ISimCardService service) : ControllerBase
{
    [HttpGet("{id:guid}", Name = nameof(GetSimCard))]
    public async Task<ActionResult<SimCardModel>> GetSimCard(Guid id, CancellationToken cancellationToken = default)
    {
        var card = await service.GetAsync(id, cancellationToken);
        if (card is null)
        {
            return NotFound();
        }

        return Ok(card);
    }

    [HttpGet(Name = nameof(ListSimCard))]
    public async Task<ActionResult<List<SimCardModel>>> ListSimCard(CancellationToken cancellationToken = default)
    {
        var list = await service.ListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost(Name = nameof(CreateSimCard))]
    public async Task<ActionResult<SimCardModel>> CreateSimCard(SimCardModel request, CancellationToken cancellationToken)
    {
        var created = await service.Create(request, cancellationToken);
        return CreatedAtAction(nameof(GetSimCard), new { id = created.Id }, created);
    }
}
