using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FaceRank.Core.Data;
using FaceRank.Core.Models;

namespace FaceRank.Web.Pages.Leaderboard;

public class IndexModel : PageModel
{
    private readonly FaceRankDbContext _db;

    public IndexModel(FaceRankDbContext db) => _db = db;

    public string Gender { get; set; } = "Male";
    public List<Person> People { get; set; } = [];

    public async Task OnGetAsync(string gender)
    {
        Gender = string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase) ? "Female" : "Male";
        People = await _db.People
            .Where(p => p.Gender == Gender)
            .OrderByDescending(p => p.EloRating)
            .ThenByDescending(p => p.VotesCount)
            .ToListAsync();
    }
}
