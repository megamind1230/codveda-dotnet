using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FaceRank.Core.Data;

namespace FaceRank.Web.Pages;

public class IndexModel : PageModel
{
    private readonly FaceRankDbContext _db;

    public IndexModel(FaceRankDbContext db) => _db = db;

    public int TotalPeople { get; set; }
    public int TotalVotes { get; set; }

    public async Task OnGetAsync()
    {
        TotalPeople = await _db.People.CountAsync();
        TotalVotes = await _db.Votes.CountAsync();
    }
}
