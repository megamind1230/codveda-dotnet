using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using CounterPick.Core.Data;

namespace CounterPick.Api.Controllers;

[ApiController]
[Route("api/heroes")]
public class HeroesController : ControllerBase
{
    private readonly AppDbContext _db;

    public HeroesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [EnableRateLimiting("SearchLimit")]
    public async Task<IActionResult> GetAll()
    {
        var heroes = await _db.Heroes
            .OrderBy(h => h.LocalizedName)
            .Select(h => new
            {
                h.Id,
                h.LocalizedName,
                h.PrimaryAttr,
                h.AttackType,
                Roles = h.Roles,
                h.ImageUrl
            })
            .ToListAsync();

        return Ok(heroes);
    }

    [HttpGet("{id}")]
    [EnableRateLimiting("SearchLimit")]
    public async Task<IActionResult> GetById(int id)
    {
        var hero = await _db.Heroes
            .Where(h => h.Id == id)
            .Select(h => new
            {
                h.Id,
                h.LocalizedName,
                h.PrimaryAttr,
                h.AttackType,
                Roles = h.Roles,
                h.ImageUrl
            })
            .FirstOrDefaultAsync();

        if (hero is null)
            return NotFound();

        return Ok(hero);
    }
}
