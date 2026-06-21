namespace FaceRank.Core.Models;

public class Vote
{
    public int Id { get; set; }
    public int WinnerId { get; set; }
    public int LoserId { get; set; }
    public string? VoterIp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Person Winner { get; set; } = null!;
    public Person Loser { get; set; } = null!;
}
