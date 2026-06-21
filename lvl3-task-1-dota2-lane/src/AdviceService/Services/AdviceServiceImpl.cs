using Grpc.Core;
using DotaLane.AdviceService;

namespace DotaLane.AdviceService.Services;

public class AdviceServiceImpl : AdviceService.AdviceServiceBase
{
    private readonly ILogger<AdviceServiceImpl> _logger;

    // baka: threshold for a "meaningful" stat lead.
    // baka: 0.15 = 15% difference is noticeable in laning phase.
    // baka: tuned for ~30 heroes producing believable verdicts.
    private const double Threshold = 0.15;

    public AdviceServiceImpl(ILogger<AdviceServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<AdviceResponse> GetLaneAdvice(AdviceRequest request, ServerCallContext context)
    {
        var yours = request.YourHero;
        var enemy = request.EnemyHero;

        _logger.LogInformation(
            "Advice requested: {Your} vs {Enemy} ({Lane})",
            yours.Name, enemy.Name, request.Lane);

        // baka: compare each stat, count who leads.
        // baka: normalized diff prevents huge numbers (range 950 vs 150)
        // baka: from dominating the verdict while tiny differences get ignored.
        var results = new List<StatResult>();
        int advantages = 0, disadvantages = 0;

        // baka: lowerIsBetter: for BAT, lower = faster = advantage.
        void Compare(string name, double yourVal, double enemyVal, bool lowerIsBetter = false)
        {
            var rawDiff = yourVal - enemyVal;
            var effectiveDiff = lowerIsBetter ? -rawDiff : rawDiff;
            var max = Math.Max(Math.Abs(yourVal), Math.Abs(enemyVal));
            var normalized = max > 0 ? effectiveDiff / max : 0;

            if (normalized > Threshold)
            {
                advantages++;
                results.Add(new StatResult(name, "you", yourVal, enemyVal));
            }
            else if (normalized < -Threshold)
            {
                disadvantages++;
                results.Add(new StatResult(name, "enemy", yourVal, enemyVal));
            }
        }

        // baka: damage is the average of min+max for comparison.
        Compare("Damage", (yours.DamageMin + yours.DamageMax) / 2.0,
            (enemy.DamageMin + enemy.DamageMax) / 2.0);
        Compare("Attack Range", yours.AttackRange, enemy.AttackRange);
        Compare("Base Attack Time", yours.BaseAttackTime, enemy.BaseAttackTime, lowerIsBetter: true);
        Compare("Armor", yours.Armor, enemy.Armor);
        Compare("Move Speed", yours.MoveSpeed, enemy.MoveSpeed);
        Compare("Day Vision", yours.DayVision, enemy.DayVision);
        Compare("Night Vision", yours.NightVision, enemy.NightVision);
        Compare("Int Gain", yours.IntGain, enemy.IntGain);
        Compare("Agi Gain", yours.AgiGain, enemy.AgiGain);

        var yourLeads = results.Where(r => r.Leader == "you").Select(r => r.Name).ToList();
        var enemyLeads = results.Where(r => r.Leader == "enemy").Select(r => r.Name).ToList();

        // baka: verdict requires a 2-stat lead gap to be decisive.
        // baka: otherwise it's "survive" — playable but you need to be careful.
        string verdict, reason;
        if (advantages >= disadvantages + 2)
        {
            verdict = "stomp";
            reason = yourLeads.Count > 0
                ? $"You lead in {string.Join(", ", yourLeads.Take(2))}"
                : "You have the edge in most categories";
        }
        else if (disadvantages >= advantages + 2)
        {
            verdict = "avoid";
            reason = enemyLeads.Count > 0
                ? $"You're outmatched in {string.Join(", ", enemyLeads.Take(2))}"
                : "You're behind in most categories";
        }
        else
        {
            verdict = "survive";
            var yourBest = yourLeads.FirstOrDefault();
            var enemyBest = enemyLeads.FirstOrDefault();
            reason = "Play safe";
            if (yourBest != null && enemyBest != null)
                reason = $"Play safe — your {yourBest} is your edge, avoid trading in {enemyBest}";
            else if (yourBest != null)
                reason = $"Play safe — your {yourBest} is your edge";
            else if (enemyBest != null)
                reason = $"Play safe — avoid trading, they lead in {enemyBest}";
        }

        var response = new AdviceResponse
        {
            Verdict = verdict,
            Reason = reason,
        };
        response.Advantages.AddRange(yourLeads);
        response.Disadvantages.AddRange(enemyLeads);

        _logger.LogInformation(
            "Advice result: {Verdict} — {Reason} (advantages={Advs}, disadvantages={Disadvs})",
            verdict, reason, advantages, disadvantages);

        return Task.FromResult(response);
    }

    private record StatResult(string Name, string Leader, double YourVal, double EnemyVal);
}
