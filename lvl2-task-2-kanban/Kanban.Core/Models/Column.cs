namespace Kanban.Core.Models;

public class Column
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Card> Cards { get; set; } = new();
}
