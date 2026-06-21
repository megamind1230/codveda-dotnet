using Grpc.Core;
using DotaLane.MatchupService;
using DotaLane.MatchupService.Models;
using DotaLane.AdviceService;
using AdviceServiceClient = DotaLane.AdviceService.AdviceService.AdviceServiceClient;

namespace DotaLane.MatchupService.Services;

// baka: orchestrates HeroService (REST) + AdviceService (gRPC) into one response.
public class MatchupServiceImpl : MatchupService.MatchupServiceBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AdviceServiceClient _adviceClient;
    private readonly ILogger<MatchupServiceImpl> _logger;

    public MatchupServiceImpl(
        IHttpClientFactory httpClientFactory,
        AdviceServiceClient adviceClient,
        ILogger<MatchupServiceImpl> logger)
    {
        _httpClientFactory = httpClientFactory;
        _adviceClient = adviceClient;
        _logger = logger;
    }

    public override async Task<MatchupResponse> GetMatchup(
        MatchupRequest request, ServerCallContext context)
    {
        _logger.LogInformation(
            "Matchup requested: hero {YourId} vs {EnemyId} ({Lane})",
            request.YourHeroId, request.EnemyHeroId, request.Lane);

        // baka: fetch both heroes from HeroService REST API in parallel.
        var yourTask = FetchHeroAsync(request.YourHeroId);
        var enemyTask = FetchHeroAsync(request.EnemyHeroId);
        await Task.WhenAll(yourTask, enemyTask);

        var yourHero = yourTask.Result;
        var enemyHero = enemyTask.Result;

        if (yourHero == null || enemyHero == null)
        {
            _logger.LogError("Hero not found: your={YourId} enemy={EnemyId}",
                request.YourHeroId, request.EnemyHeroId);
            throw new RpcException(new Status(
                StatusCode.NotFound, "One or both heroes not found"));
        }

        // baka: build stat comparison table.
        var stats = BuildStatLines(yourHero, enemyHero);

        // baka: call AdviceService via gRPC for lane verdict.
        var adviceResponse = await _adviceClient.GetLaneAdviceAsync(
            new AdviceRequest
            {
                YourHero = MapToHeroStats(yourHero),
                EnemyHero = MapToHeroStats(enemyHero),
                Lane = request.Lane
            });

        var response = new MatchupResponse
        {
            YourHeroName = yourHero.Name,
            EnemyHeroName = enemyHero.Name,
            Lane = request.Lane,
            Verdict = adviceResponse.Verdict,
            AdviceReason = adviceResponse.Reason,
        };
        response.Stats.AddRange(stats);
        response.Advantages.AddRange(adviceResponse.Advantages);
        response.Disadvantages.AddRange(adviceResponse.Disadvantages);

        _logger.LogInformation(
            "Matchup result: {Your} vs {Enemy} → {Verdict}",
            yourHero.Name, enemyHero.Name, adviceResponse.Verdict);

        return response;
    }

    private async Task<HeroDto?> FetchHeroAsync(int heroId)
    {
        var client = _httpClientFactory.CreateClient("HeroService");
        var url = $"api/heroes/{heroId}";
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<HeroDto>();
    }

    private static List<HeroStatLine> BuildStatLines(HeroDto y, HeroDto e)
    {
        return new List<HeroStatLine>
        {
            MakeStat("Damage", $"{y.DamageMin}-{y.DamageMax}", $"{e.DamageMin}-{e.DamageMax}",
                Avg(y.DamageMin, y.DamageMax), Avg(e.DamageMin, e.DamageMax)),
            MakeStat("Primary Attribute", y.PrimaryAttribute, e.PrimaryAttribute),
            MakeStat("Attack Range", $"{y.AttackRange}", $"{e.AttackRange}",
                y.AttackRange, e.AttackRange),
            MakeStat("Base Attack Time", $"{y.BaseAttackTime:F1}", $"{e.BaseAttackTime:F1}",
                y.BaseAttackTime, e.BaseAttackTime, lowerIsBetter: true),
            MakeStat("Armor", $"{y.Armor:F1}", $"{e.Armor:F1}",
                y.Armor, e.Armor),
            MakeStat("Move Speed", $"{y.MoveSpeed}", $"{e.MoveSpeed}",
                y.MoveSpeed, e.MoveSpeed),
            MakeStat("Day Vision", $"{y.DayVision}", $"{e.DayVision}",
                y.DayVision, e.DayVision),
            MakeStat("Night Vision", $"{y.NightVision}", $"{e.NightVision}",
                y.NightVision, e.NightVision),
            MakeStat("Str Gain", $"{y.StrGain:F1}", $"{e.StrGain:F1}",
                y.StrGain, e.StrGain),
            MakeStat("Agi Gain", $"{y.AgiGain:F1}", $"{e.AgiGain:F1}",
                y.AgiGain, e.AgiGain),
            MakeStat("Int Gain", $"{y.IntGain:F1}", $"{e.IntGain:F1}",
                y.IntGain, e.IntGain),
        };
    }

    // baka: numeric stats get "you"/"enemy"/"tie" based on which value is higher.
    // baka: non-numeric stats (primary attribute) are always "tie".
    // baka: lowerIsBetter is used for BAT (lower = faster)
    private static HeroStatLine MakeStat(string name, string yVal, string eVal,
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
        return new HeroStatLine
        {
            StatName = name,
            YourValue = yVal,
            EnemyValue = eVal,
            Advantage = adv,
        };
    }

    private static double Avg(int a, int b) => (a + b) / 2.0;

    // baka: map HeroDto → HeroStats (advice.proto message) for the gRPC call.
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
