using Microsoft.AspNetCore.Mvc;
using DotaLane.HeroService.Data;
using DotaLane.HeroService.Services;

namespace DotaLane.HeroService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HeroesController : ControllerBase
{
    private readonly HeroRepository _repo;
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<HeroesController> _logger;

    public HeroesController(HeroRepository repo, RabbitMqPublisher publisher, ILogger<HeroesController> logger)
    {
        _repo = repo;
        _publisher = publisher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? lane)
    {
        _logger.LogInformation("GetAll heroes lane={Lane}", lane);

        if (!string.IsNullOrEmpty(lane))
            return Ok(await _repo.GetByLaneAsync(lane));

        return Ok(await _repo.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("GetHero id={Id}", id);

        var hero = await _repo.GetByIdAsync(id);
        if (hero == null)
            return NotFound();

        return Ok(hero);
    }

    [HttpPost("reload")]
    public async Task<IActionResult> Reload()
    {
        _logger.LogInformation("Reload triggered — publishing HeroStatsUpdated event");

        var allHeroes = await _repo.GetAllAsync();
        var heroIds = allHeroes.Select(h => h.Id).ToList();

        await _publisher.PublishHeroStatsUpdatedAsync(heroIds);

        _logger.LogInformation("Reload complete — event published for {Count} heroes", heroIds.Count);
        return Ok(new { message = "HeroStatsUpdated event published", heroCount = heroIds.Count });
    }
}
