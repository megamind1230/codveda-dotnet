using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CounterPick.Core.Data;
using CounterPick.Core.Models;

namespace CounterPick.Api.Controllers;

[ApiController]
[Route("api/heroes/{heroId}/counters")]
public class CounterSuggestionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CounterSuggestionsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetCounters(int heroId)
    {
        var exists = await _db.Heroes.AnyAsync(h => h.Id == heroId);
        if (!exists) return NotFound();

        var counters = await _db.CounterSuggestions
            .Where(cs => cs.HeroId == heroId)
            .Include(cs => cs.CounterHero)
            .Include(cs => cs.Comments)
                .ThenInclude(c => c.Likes)
            .Select(cs => new
            {
                cs.Id,
                CounterHero = new
                {
                    cs.CounterHero.Id,
                    cs.CounterHero.LocalizedName,
                    cs.CounterHero.ImageUrl
                },
                TopComment = cs.Comments
                    .OrderByDescending(c => c.LikeCount)
                    .Select(c => new { c.Content, c.LikeCount })
                    .FirstOrDefault(),
                CommentCount = cs.Comments.Count
            })
            .ToListAsync();

        return Ok(counters);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddCounter(int heroId, [FromBody] AddCounterDto dto)
    {
        if (heroId == dto.CounterHeroId)
            return BadRequest(new { message = "A hero cannot counter itself" });

        var heroExists = await _db.Heroes.AnyAsync(h => h.Id == heroId);
        var counterExists = await _db.Heroes.AnyAsync(h => h.Id == dto.CounterHeroId);
        if (!heroExists || !counterExists) return NotFound("Hero not found");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var suggestion = new CounterSuggestion
        {
            HeroId = heroId,
            CounterHeroId = dto.CounterHeroId,
            Reason = dto.Reason,
            SuggestedById = userId
        };

        _db.CounterSuggestions.Add(suggestion);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCounters), new { heroId }, suggestion);
    }
}

public class AddCounterDto
{
    public int CounterHeroId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
