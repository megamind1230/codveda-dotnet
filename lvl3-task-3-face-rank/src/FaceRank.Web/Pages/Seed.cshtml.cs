using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FaceRank.Core.Data;

namespace FaceRank.Web.Pages;

public class SeedModel : PageModel
{
    private readonly FaceRankDbContext _db;

    public SeedModel(FaceRankDbContext db) => _db = db;

    public bool AlreadySeeded { get; set; }
    public bool Done { get; set; }
    public int TotalPeople { get; set; }
    public int TotalVotes { get; set; }

    public async Task OnGetAsync()
    {
        TotalPeople = await _db.People.CountAsync();
        TotalVotes = await _db.Votes.CountAsync();
        AlreadySeeded = TotalPeople > 0;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        TotalPeople = await _db.People.CountAsync();
        if (TotalPeople > 0)
        {
            AlreadySeeded = true;
            return Page();
        }

        await DbSeeder.SeedAsync(_db);
        Done = true;
        TotalPeople = await _db.People.CountAsync();
        TotalVotes = await _db.Votes.CountAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        _db.People.RemoveRange(await _db.People.ToListAsync());
        _db.Votes.RemoveRange(await _db.Votes.ToListAsync());
        await _db.SaveChangesAsync();

        await DbSeeder.SeedAsync(_db);
        Done = true;
        TotalPeople = await _db.People.CountAsync();
        TotalVotes = await _db.Votes.CountAsync();
        return Page();
    }
}
