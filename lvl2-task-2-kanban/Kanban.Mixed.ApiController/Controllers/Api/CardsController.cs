using Microsoft.AspNetCore.Mvc;
using Kanban.Core.Services;
using Kanban.Core.Models;

namespace Kanban.Mixed.ApiController.Controllers.Api;

[ApiController]
[Route("api/cards")]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;
    public CardsController(ICardService cardService) => _cardService = cardService;

    [HttpGet("by-column/{columnId}")]
    public async Task<IActionResult> GetByColumn(int columnId)
        => Ok(await _cardService.GetByColumnIdAsync(columnId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Card card)
    {
        var created = await _cardService.CreateAsync(card);
        return CreatedAtAction(null, new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Card card)
    {
        card.Id = id;
        return Ok(await _cardService.UpdateAsync(card));
    }

    [HttpPatch("{id}/move")]
    public async Task<IActionResult> Move(int id, [FromBody] MoveCardRequest dto)
    {
        await _cardService.MoveCardAsync(id, dto.TargetColumnId, dto.NewOrder);
        return Ok();
    }
}
