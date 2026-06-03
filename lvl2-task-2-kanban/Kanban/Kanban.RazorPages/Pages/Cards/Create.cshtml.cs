using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Kanban.Core.Services;
using Kanban.Core.Models;

namespace Kanban.RazorPages.Pages.Cards;

public class CreateModel : PageModel
{
    private readonly ICardService _cardService;
    [BindProperty]
    public Card Card { get; set; } = new();

    public CreateModel(ICardService cardService) => _cardService = cardService;

    public void OnGet(int columnId)
    {
        Card.ColumnId = columnId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _cardService.CreateAsync(Card);
        return RedirectToPage("/Board/Index");
    }
}
