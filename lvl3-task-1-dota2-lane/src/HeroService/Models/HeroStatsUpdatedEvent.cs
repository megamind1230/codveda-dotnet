using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotaLane.HeroService.Models;

public class HeroStatsUpdatedEvent
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = "HeroStatsUpdated";

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");

    [JsonPropertyName("heroIds")]
    public List<int> HeroIds { get; set; } = new();

    public string ToJson() =>
        JsonSerializer.Serialize(this);
}
