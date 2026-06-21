using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CounterPick.Core.Data;
using CounterPick.Core.DTOs;
using CounterPick.Core.Models;

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

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateHeroDto dto)
    {
        var exists = await _db.Heroes.AnyAsync(h => h.Id == dto.Id);
        if (exists)
            return Conflict(new { message = $"A hero with ID {dto.Id} already exists" });

        var hero = new Hero
        {
            Id = dto.Id,
            Name = dto.Name,
            LocalizedName = dto.LocalizedName,
            PrimaryAttr = dto.PrimaryAttr,
            AttackType = dto.AttackType,
            Roles = dto.Roles,
            ImageUrl = dto.ImageUrl
        };

        _db.Heroes.Add(hero);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = hero.Id }, new
        {
            hero.Id,
            hero.LocalizedName,
            hero.PrimaryAttr,
            hero.AttackType,
            Roles = hero.Roles,
            hero.ImageUrl
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHeroDto dto)
    {
        var hero = await _db.Heroes.FindAsync(id);
        if (hero is null)
            return NotFound();

        if (dto.Name is not null)
            hero.Name = dto.Name;
        if (dto.LocalizedName is not null)
            hero.LocalizedName = dto.LocalizedName;
        if (dto.PrimaryAttr is not null)
            hero.PrimaryAttr = dto.PrimaryAttr;
        if (dto.AttackType is not null)
            hero.AttackType = dto.AttackType;
        if (dto.Roles is not null)
            hero.Roles = dto.Roles;
        if (dto.ImageUrl is not null)
            hero.ImageUrl = dto.ImageUrl;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            hero.Id,
            hero.LocalizedName,
            hero.PrimaryAttr,
            hero.AttackType,
            Roles = hero.Roles,
            hero.ImageUrl
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var hero = await _db.Heroes.FindAsync(id);
        if (hero is null)
            return NotFound();

        var suggestions = await _db.CounterSuggestions
            .Where(cs => cs.HeroId == id || cs.CounterHeroId == id)
            .ToListAsync();

        _db.CounterSuggestions.RemoveRange(suggestions);
        _db.Heroes.Remove(hero);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
