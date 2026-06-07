using Hollis.CellularGateway.WebApi.Conventions;
using Hollis.CellularGateway.WebApi.Models.ShortMessage;
using Hollis.CellularGateway.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace Hollis.CellularGateway.WebApi.Controllers;

[Authorize]
[ApiController]
[ChildResourceOf(typeof(SimCardsController), ParentIdParam = "simCardId")]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public class ShortMessagesController(IShortMessageService service) : ControllerBase
{
    [HttpGet(Name = nameof(ListMessages))]
    public async Task<ActionResult<IEnumerable<ShortMessageModel>>> ListMessages(Guid simCardId, CancellationToken cancellationToken)
    {
        var list = await service.ListAsync(simCardId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}", Name = nameof(GetMessage))]
    public async Task<ActionResult<ShortMessageModel>> GetMessage(Guid simCardId, Guid id, CancellationToken cancellationToken)
    {
        var message = await service.GetAsync(simCardId, id, cancellationToken);
        if (message is null)
        {
            return NotFound();
        }

        return Ok(message);
    }

    [HttpPost(Name = nameof(CreateMessage))]
    public async Task<ActionResult<ShortMessageModel>> CreateMessage(Guid simCardId, ShortMessageModel request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(simCardId, request, cancellationToken);
        return CreatedAtAction(nameof(GetMessage), new { simCardId, id = created.Id }, created);
    }
}
