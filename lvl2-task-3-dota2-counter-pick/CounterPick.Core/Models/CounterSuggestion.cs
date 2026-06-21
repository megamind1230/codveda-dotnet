namespace CounterPick.Core.Models;

public class CounterSuggestion
{
    public int Id { get; set; }
    public int HeroId { get; set; }
    public int CounterHeroId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string SuggestedById { get; set; } = string.Empty;

    public Hero Hero { get; set; } = null!;
    public Hero CounterHero { get; set; } = null!;
}
