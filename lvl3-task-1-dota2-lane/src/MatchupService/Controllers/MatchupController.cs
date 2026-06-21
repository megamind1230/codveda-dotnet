using Microsoft.AspNetCore.Mvc;
using DotaLane.MatchupService.Models;
using DotaLane.AdviceService;
using AdviceServiceClient = DotaLane.AdviceService.AdviceService.AdviceServiceClient;

namespace DotaLane.MatchupService.Controllers;

// baka: REST endpoint for API Gateway. Internally calls HeroService (REST)
// baka: and AdviceService (gRPC) — same logic as the gRPC MatchupServiceImpl.
[ApiController]
[Route("api/[controller]")]
public class MatchupController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AdviceServiceClient _adviceClient;
    private readonly ILogger<MatchupController> _logger;

    public MatchupController(
        IHttpClientFactory httpClientFactory,
        AdviceServiceClient adviceClient,
        ILogger<MatchupController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _adviceClient = adviceClient;
        _logger = logger;
    }

    [HttpGet("{yourHeroId}/{enemyHeroId}")]
    public async Task<IActionResult> GetMatchup(int yourHeroId, int enemyHeroId, [FromQuery] string lane = "mid")
    {
        _logger.LogInformation("REST matchup: {Y} vs {E} ({Lane})", yourHeroId, enemyHeroId, lane);

        var yourHero = await FetchHeroAsync(yourHeroId);
        var enemyHero = await FetchHeroAsync(enemyHeroId);
        if (yourHero == null || enemyHero == null)
            return NotFound("One or both heroes not found");

        var advice = await _adviceClient.GetLaneAdviceAsync(new AdviceRequest
        {
            YourHero = MapToHeroStats(yourHero),
            EnemyHero = MapToHeroStats(enemyHero),
            Lane = lane,
        });

        var stats = BuildStatLines(yourHero, enemyHero);

        return Ok(new
        {
            yourHeroName = yourHero.Name,
            enemyHeroName = enemyHero.Name,
            lane,
            verdict = advice.Verdict,
            adviceReason = advice.Reason,
            advantages = advice.Advantages,
            disadvantages = advice.Disadvantages,
            stats,
        });
    }

    private async Task<HeroDto?> FetchHeroAsync(int id)
    {
        var client = _httpClientFactory.CreateClient("HeroService");
        var url = $"api/heroes/{id}";
        var resp = await client.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<HeroDto>();
    }

    private static List<object> BuildStatLines(HeroDto y, HeroDto e)
    {
        return new List<object>
        {
            MakeStat("Damage", $"{y.DamageMin}-{y.DamageMax}", $"{e.DamageMin}-{e.DamageMax}",
                Avg(y.DamageMin, y.DamageMax), Avg(e.DamageMin, e.DamageMax)),
            MakeStat("Primary Attribute", y.PrimaryAttribute, e.PrimaryAttribute),
            MakeStat("Attack Range", $"{y.AttackRange}", $"{e.AttackRange}", y.AttackRange, e.AttackRange),
            MakeStat("Base Attack Time", $"{y.BaseAttackTime:F1}", $"{e.BaseAttackTime:F1}",
                y.BaseAttackTime, e.BaseAttackTime, lowerIsBetter: true),
            MakeStat("Armor", $"{y.Armor:F1}", $"{e.Armor:F1}", y.Armor, e.Armor),
            MakeStat("Move Speed", $"{y.MoveSpeed}", $"{e.MoveSpeed}", y.MoveSpeed, e.MoveSpeed),
            MakeStat("Day Vision", $"{y.DayVision}", $"{e.DayVision}", y.DayVision, e.DayVision),
            MakeStat("Night Vision", $"{y.NightVision}", $"{e.NightVision}", y.NightVision, e.NightVision),
            MakeStat("Str Gain", $"{y.StrGain:F1}", $"{e.StrGain:F1}", y.StrGain, e.StrGain),
            MakeStat("Agi Gain", $"{y.AgiGain:F1}", $"{e.AgiGain:F1}", y.AgiGain, e.AgiGain),
            MakeStat("Int Gain", $"{y.IntGain:F1}", $"{e.IntGain:F1}", y.IntGain, e.IntGain),
        };
    }

    private static object MakeStat(string name, string yVal, string eVal,
        double? yNum = null, double? eNum = null, bool lowerIsBetter = false)
    {
        var adv = "tie";
        if (yNum.HasValue && eNum.HasValue)
        {
            if (lowerIsBetter)
            {
                if (yNum < eNum) adv = "you";
                else if (eNum < yNum) adv = "enemy";
            }
            else
            {
                if (yNum > eNum) adv = "you";
                else if (eNum > yNum) adv = "enemy";
            }
        }
        return new { statName = name, yourValue = yVal, enemyValue = eVal, advantage = adv };
    }

    private static double Avg(int a, int b) => (a + b) / 2.0;

    private static HeroStats MapToHeroStats(HeroDto h)
    {
        return new HeroStats
        {
            HeroId = h.Id,
            Name = h.Name,
            DamageMin = h.DamageMin,
            DamageMax = h.DamageMax,
            AttackRange = h.AttackRange,
            BaseAttackTime = h.BaseAttackTime,
            Armor = h.Armor,
            MoveSpeed = h.MoveSpeed,
            DayVision = h.DayVision,
            NightVision = h.NightVision,
            IntGain = h.IntGain,
            AgiGain = h.AgiGain,
            PrimaryAttr = h.PrimaryAttribute,
        };
    }
}
