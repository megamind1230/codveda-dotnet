using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Kanban.Core.Services;
using Kanban.Core.Models;

namespace Kanban.RazorPages.Pages.Cards;

public class DeleteModel : PageModel
{
    private readonly ICardService _cardService;
    public Card Card { get; set; } = new();

    public DeleteModel(ICardService cardService) => _cardService = cardService;

    public async Task OnGetAsync(int id)
    {
        Card = await _cardService.GetByIdAsync(id) ?? new();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        await _cardService.DeleteAsync(id);
        return RedirectToPage("/Board/Index");
    }
}
