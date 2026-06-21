using System.Text.Json.Serialization;

namespace DotaLane.Frontend.Models;

public class MatchupResponse
{
    [JsonPropertyName("yourHeroName")]
    public string YourHeroName { get; set; } = "";

    [JsonPropertyName("enemyHeroName")]
    public string EnemyHeroName { get; set; } = "";

    [JsonPropertyName("lane")]
    public string Lane { get; set; } = "";

    [JsonPropertyName("verdict")]
    public string Verdict { get; set; } = "";

    [JsonPropertyName("adviceReason")]
    public string AdviceReason { get; set; } = "";

    [JsonPropertyName("advantages")]
    public List<string> Advantages { get; set; } = new();

    [JsonPropertyName("disadvantages")]
    public List<string> Disadvantages { get; set; } = new();

    [JsonPropertyName("stats")]
    public List<StatLineDto> Stats { get; set; } = new();
}
