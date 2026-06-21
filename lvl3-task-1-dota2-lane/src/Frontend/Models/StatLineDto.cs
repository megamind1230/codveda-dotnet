using System.Text.Json.Serialization;

namespace DotaLane.Frontend.Models;

public class StatLineDto
{
    [JsonPropertyName("statName")]
    public string StatName { get; set; } = "";

    [JsonPropertyName("yourValue")]
    public string YourValue { get; set; } = "";

    [JsonPropertyName("enemyValue")]
    public string EnemyValue { get; set; } = "";

    [JsonPropertyName("advantage")]
    public string Advantage { get; set; } = "tie";
}
