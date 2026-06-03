using Kanban.Core.Models;

namespace Kanban.Core.Services;

public interface ICardService
{
    Task<List<Card>> GetByColumnIdAsync(int columnId);
    Task<Card?> GetByIdAsync(int id);
    Task<Card> CreateAsync(Card card);
    Task<Card> UpdateAsync(Card card);
    Task DeleteAsync(int id);
    Task MoveCardAsync(int id, int targetColumnId, int newOrder);
}
