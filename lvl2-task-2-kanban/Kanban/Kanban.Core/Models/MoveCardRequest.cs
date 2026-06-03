namespace Kanban.Core.Models;

public record MoveCardRequest(int TargetColumnId, int NewOrder);
