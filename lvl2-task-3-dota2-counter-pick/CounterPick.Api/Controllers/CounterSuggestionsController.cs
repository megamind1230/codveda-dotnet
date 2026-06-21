using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CounterPick.Api.Authorization;
using CounterPick.Core.Constants;
using CounterPick.Core.Data;
using CounterPick.Core.Models;

namespace CounterPick.Api.Controllers;

[ApiController]
[Route("api/heroes/{heroId}/counters")]
public class CounterSuggestionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationService _auth;
    private readonly UserManager<IdentityUser> _userManager;

    public CounterSuggestionsController(AppDbContext db, IAuthorizationService auth, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _auth = auth;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetCounters(int heroId)
    {
        var exists = await _db.Heroes.AnyAsync(h => h.Id == heroId);
        if (!exists) return NotFound();

        var users = await _userManager.Users.ToListAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.UserName ?? u.Id);

        var counters = await _db.CounterSuggestions
            .Where(cs => cs.HeroId == heroId)
            .Include(cs => cs.CounterHero)
            .Select(cs => new
            {
                cs.Id,
                cs.Reason,
                cs.SuggestedById,
                CounterHero = new
                {
                    cs.CounterHero.Id,
                    cs.CounterHero.LocalizedName,
                    cs.CounterHero.ImageUrl
                }
            })
            .ToListAsync();

        var result = counters.Select(c => new
        {
            c.Id,
            c.Reason,
            c.SuggestedById,
            SuggestedByUserName = userMap.GetValueOrDefault(c.SuggestedById, c.SuggestedById),
            c.CounterHero
        });

        return Ok(result);
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

    //#baka uses resource-based auth: the policy runs CounterSuggestionOwnerHandler with the actual suggestion as the resource
    [HttpPut("{suggestionId}")]
    [Authorize]
    public async Task<IActionResult> UpdateCounter(int heroId, int suggestionId, [FromBody] UpdateCounterDto dto)
    {
        var suggestion = await _db.CounterSuggestions.FindAsync(suggestionId);
        if (suggestion is null) return NotFound();
        if (suggestion.HeroId != heroId) return BadRequest();

        if (heroId == dto.CounterHeroId)
            return BadRequest(new { message = "A hero cannot counter itself" });

        var authResult = await _auth.AuthorizeAsync(User, suggestion, AppPolicies.OwnsSuggestion);
        if (!authResult.Succeeded)
            return Forbid();

        suggestion.CounterHeroId = dto.CounterHeroId;
        suggestion.Reason = dto.Reason;
        await _db.SaveChangesAsync();

        return Ok(new { suggestion.Id, suggestion.Reason, suggestion.CounterHeroId });
    }

    [HttpDelete("{suggestionId}")]
    [Authorize]
    public async Task<IActionResult> DeleteCounter(int heroId, int suggestionId)
    {
        var suggestion = await _db.CounterSuggestions.FindAsync(suggestionId);
        if (suggestion is null) return NotFound();
        if (suggestion.HeroId != heroId) return BadRequest();

        var authResult = await _auth.AuthorizeAsync(User, suggestion, AppPolicies.OwnsSuggestion);
        if (!authResult.Succeeded)
            return Forbid();

        _db.CounterSuggestions.Remove(suggestion);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public class AddCounterDto
{
    public int CounterHeroId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class UpdateCounterDto
{
    public int CounterHeroId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
