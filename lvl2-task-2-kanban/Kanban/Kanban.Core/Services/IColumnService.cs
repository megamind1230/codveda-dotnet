using Kanban.Core.Models;

namespace Kanban.Core.Services;

public interface IColumnService
{
    Task<List<Column>> GetAllAsync();
    Task<Column?> GetByIdAsync(int id);
    Task<Column> CreateAsync(Column column);
    Task<Column> UpdateAsync(Column column);
    Task DeleteAsync(int id);
    Task UpdateOrderAsync(int id, int newOrder);
}
