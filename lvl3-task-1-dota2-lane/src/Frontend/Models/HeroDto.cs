using System.Text.Json.Serialization;

namespace DotaLane.Frontend.Models;

public class HeroDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("primaryAttribute")]
    public string PrimaryAttribute { get; set; } = "";

    [JsonPropertyName("damageMin")]
    public int DamageMin { get; set; }

    [JsonPropertyName("damageMax")]
    public int DamageMax { get; set; }

    [JsonPropertyName("attackRange")]
    public int AttackRange { get; set; }

    [JsonPropertyName("baseAttackTime")]
    public double BaseAttackTime { get; set; }

    [JsonPropertyName("armor")]
    public double Armor { get; set; }

    [JsonPropertyName("moveSpeed")]
    public int MoveSpeed { get; set; }

    [JsonPropertyName("dayVision")]
    public int DayVision { get; set; }

    [JsonPropertyName("nightVision")]
    public int NightVision { get; set; }

    [JsonPropertyName("strGain")]
    public double StrGain { get; set; }

    [JsonPropertyName("agiGain")]
    public double AgiGain { get; set; }

    [JsonPropertyName("intGain")]
    public double IntGain { get; set; }

    [JsonPropertyName("baseStr")]
    public int BaseStr { get; set; }

    [JsonPropertyName("baseAgi")]
    public int BaseAgi { get; set; }

    [JsonPropertyName("baseInt")]
    public int BaseInt { get; set; }

    [JsonPropertyName("lane")]
    public string Lane { get; set; } = "";
}
