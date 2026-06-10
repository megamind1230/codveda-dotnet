using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Kanban.Core.Models;

public class Card
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public int ColumnId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [ValidateNever]
    public Column Column { get; set; } = null!;
}
