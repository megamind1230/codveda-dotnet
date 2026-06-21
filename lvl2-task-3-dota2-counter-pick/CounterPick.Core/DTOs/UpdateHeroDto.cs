namespace CounterPick.Core.DTOs;

public class UpdateHeroDto
{
    public string? Name { get; set; }
    public string? LocalizedName { get; set; }
    public string? PrimaryAttr { get; set; }
    public string? AttackType { get; set; }
    public string? Roles { get; set; }
    public string? ImageUrl { get; set; }
}
