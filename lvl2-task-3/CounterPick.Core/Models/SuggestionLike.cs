namespace CounterPick.Core.Models;

public class SuggestionLike
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public Comment Comment { get; set; } = null!;
}
