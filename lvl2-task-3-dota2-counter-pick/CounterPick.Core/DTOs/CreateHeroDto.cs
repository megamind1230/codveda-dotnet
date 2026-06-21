namespace CounterPick.Core.DTOs;

public class CreateHeroDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LocalizedName { get; set; } = string.Empty;
    public string PrimaryAttr { get; set; } = string.Empty;
    public string AttackType { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
