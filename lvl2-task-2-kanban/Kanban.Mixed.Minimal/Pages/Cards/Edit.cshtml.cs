using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Kanban.Core.Services;
using Kanban.Core.Models;

namespace Kanban.Mixed.Minimal.Pages.Cards;

public class EditModel : PageModel
{
    private readonly ICardService _cardService;
    [BindProperty]
    public Card Card { get; set; } = new();

    public EditModel(ICardService cardService) => _cardService = cardService;

    public async Task OnGetAsync(int id)
    {
        Card = await _cardService.GetByIdAsync(id) ?? new();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _cardService.UpdateAsync(Card);
        return RedirectToPage("/Board/Index");
    }
}
