using Microsoft.AspNetCore.Mvc;
using Kanban.Core.Services;
using Kanban.Core.Models;

namespace Kanban.Mvc.Controllers;

public class CardController : Controller
{
    private readonly ICardService _cardService;
    private readonly IColumnService _columnService;
    public CardController(ICardService cardService, IColumnService columnService)
    {
        _cardService = cardService;
        _columnService = columnService;
    }

    public IActionResult Create(int columnId)
    {
        ViewBag.ColumnId = columnId;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Card card)
    {
        if (ModelState.IsValid)
        {
            await _cardService.CreateAsync(card);
            return RedirectToAction("Index", "Board");
        }
        return View(card);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var card = await _cardService.GetByIdAsync(id);
        if (card is null) return NotFound();
        return View(card);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Card card)
    {
        if (ModelState.IsValid)
        {
            await _cardService.UpdateAsync(card);
            return RedirectToAction("Index", "Board");
        }
        return View(card);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var card = await _cardService.GetByIdAsync(id);
        if (card is null) return NotFound();
        return View(card);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _cardService.DeleteAsync(id);
        return RedirectToAction("Index", "Board");
    }
}
