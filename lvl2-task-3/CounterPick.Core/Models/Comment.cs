namespace CounterPick.Core.Models;

public class Comment
{
    public int Id { get; set; }
    public int CounterSuggestionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CounterSuggestion CounterSuggestion { get; set; } = null!;
    public List<SuggestionLike> Likes { get; set; } = [];
}
