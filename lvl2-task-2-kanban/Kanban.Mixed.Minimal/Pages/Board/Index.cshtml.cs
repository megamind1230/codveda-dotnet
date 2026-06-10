using Microsoft.AspNetCore.Mvc.RazorPages;
using Kanban.Core.Services;
using Kanban.Core.Models;

namespace Kanban.Mixed.Minimal.Pages.Board;

public class IndexModel : PageModel
{
    private readonly IColumnService _columnService;
    public List<Column> Columns { get; set; } = new();

    public IndexModel(IColumnService columnService) => _columnService = columnService;

    public async Task OnGetAsync()
    {
        Columns = await _columnService.GetAllAsync();
    }
}
