using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FaceRank.Core.Data;
using FaceRank.Core.Models;
using FaceRank.Core.Services;

namespace FaceRank.Web.Pages.Vote;

public class IndexModel : PageModel
{
    private readonly FaceRankDbContext _db;

    public IndexModel(FaceRankDbContext db) => _db = db;

    public string Gender { get; set; } = "Male";
    public Person? Left { get; set; }
    public Person? Right { get; set; }
    public bool NoContestants { get; set; }

    public async Task OnGetAsync(string gender)
    {
        Gender = string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase) ? "Female" : "Male";
        await LoadPairAsync();
    }

    public async Task<IActionResult> OnPostAsync(string gender, int winnerId, int loserId)
    {
        Gender = string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase) ? "Female" : "Male";

        var lastVote = HttpContext.Session.GetString("LastVoteTime");
        if (lastVote != null)
        {
            var elapsed = DateTime.UtcNow - DateTime.Parse(lastVote, null, System.Globalization.DateTimeStyles.RoundtripKind);
            if (elapsed.TotalSeconds < 3)
                return RedirectToPage(new { gender });
        }
        HttpContext.Session.SetString("LastVoteTime", DateTime.UtcNow.ToString("O"));

        var winner = await _db.People.FindAsync(winnerId);
        var loser = await _db.People.FindAsync(loserId);
        if (winner == null || loser == null) return RedirectToPage(new { gender });

        var (newWinner, newLoser) = EloService.Calculate(winner.EloRating, loser.EloRating);
        winner.EloRating = newWinner;
        loser.EloRating = newLoser;
        winner.VotesCount++;
        loser.VotesCount++;

        _db.Votes.Add(new FaceRank.Core.Models.Vote
        {
            WinnerId = winnerId,
            LoserId = loserId,
            VoterIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _db.SaveChangesAsync();
        return RedirectToPage(new { gender });
    }

    private async Task LoadPairAsync()
    {
        var pool = await _db.People
            .Where(p => p.Gender == Gender)
            .OrderBy(_ => EF.Functions.Random())
            .Take(2)
            .ToListAsync();

        if (pool.Count < 2)
        {
            NoContestants = true;
            return;
        }

        Left = pool[0];
        Right = pool[1];
    }
}
