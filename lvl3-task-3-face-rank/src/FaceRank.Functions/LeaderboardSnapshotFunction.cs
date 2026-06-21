using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using FaceRank.Core.Data;

namespace FaceRank.Functions;

public class LeaderboardSnapshotFunction
{
    private readonly FaceRankDbContext _db;

    public LeaderboardSnapshotFunction(FaceRankDbContext db) => _db = db;

    [FunctionName("LeaderboardSnapshot")]
    public async Task Run(
        [TimerTrigger("0 0 0 * * *")] TimerInfo myTimer,
        ILogger log)
    {
        log.LogInformation($"Leaderboard snapshot triggered at: {DateTime.UtcNow}");

        var men = await _db.People
            .Where(p => p.Gender == "Male")
            .OrderByDescending(p => p.EloRating)
            .Select(p => new { p.Name, p.EloRating, p.VotesCount })
            .ToListAsync();

        var women = await _db.People
            .Where(p => p.Gender == "Female")
            .OrderByDescending(p => p.EloRating)
            .Select(p => new { p.Name, p.EloRating, p.VotesCount })
            .ToListAsync();

        log.LogInformation($"Top man: {men.FirstOrDefault()?.Name} ({men.FirstOrDefault()?.EloRating})");
        log.LogInformation($"Top woman: {women.FirstOrDefault()?.Name} ({women.FirstOrDefault()?.EloRating})");
        log.LogInformation($"Total in snapshot: {men.Count} men, {women.Count} women");
    }
}
