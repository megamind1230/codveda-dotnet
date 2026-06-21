namespace DotaLane.MatchupService.Models;

// baka: matches the JSON shape from HeroService's GET /api/heroes/{id}
public class HeroDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string PrimaryAttribute { get; set; } = "";
    public int DamageMin { get; set; }
    public int DamageMax { get; set; }
    public int AttackRange { get; set; }
    public double BaseAttackTime { get; set; }
    public double Armor { get; set; }
    public int MoveSpeed { get; set; }
    public int DayVision { get; set; }
    public int NightVision { get; set; }
    public double StrGain { get; set; }
    public double AgiGain { get; set; }
    public double IntGain { get; set; }
    public int BaseStr { get; set; }
    public int BaseAgi { get; set; }
    public int BaseInt { get; set; }
    public string Lane { get; set; } = "";
}
