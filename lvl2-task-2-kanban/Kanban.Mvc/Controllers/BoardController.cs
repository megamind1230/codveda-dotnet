using Microsoft.AspNetCore.Mvc;
using Kanban.Core.Services;
using Kanban.Core.Models;

namespace Kanban.Mvc.Controllers;

public class BoardController : Controller
{
    private readonly IColumnService _columnService;
    public BoardController(IColumnService columnService) => _columnService = columnService;

    public async Task<IActionResult> Index()
    {
        var columns = await _columnService.GetAllAsync();
        return View(columns);
    }
}
