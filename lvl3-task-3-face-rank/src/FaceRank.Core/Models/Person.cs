namespace FaceRank.Core.Models;

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string? ContactInfo { get; set; }
    public string? AvatarUrl { get; set; }
    public int EloRating { get; set; } = 1400;
    public int VotesCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
