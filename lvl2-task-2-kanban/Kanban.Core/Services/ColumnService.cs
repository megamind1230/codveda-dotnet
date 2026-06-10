using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Kanban.Core.Data;
using Kanban.Core.Models;

namespace Kanban.Core.Services;

public class ColumnService : IColumnService
{
    private readonly KanbanDbContext _db;
    private readonly ILogger<ColumnService> _logger;
    public ColumnService(KanbanDbContext db, ILogger<ColumnService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Column>> GetAllAsync()
    {
        _logger.LogDebug("Getting all columns");
        return await _db.Columns.OrderBy(c => c.Order).Include(c => c.Cards.OrderBy(ca => ca.Order)).ToListAsync();
    }

    public async Task<Column?> GetByIdAsync(int id)
    {
        var col = await _db.Columns.Include(c => c.Cards.OrderBy(ca => ca.Order)).FirstOrDefaultAsync(c => c.Id == id);
        if (col is null)
            _logger.LogWarning("Column {ColumnId} not found", id);
        else
            _logger.LogDebug("Retrieved column {ColumnId}: {Title}", id, col.Title);
        return col;
    }

    public async Task<Column> CreateAsync(Column column)
    {
        _db.Columns.Add(column);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Created column {ColumnId}: {Title}", column.Id, column.Title);
        return column;
    }

    public async Task<Column> UpdateAsync(Column column)
    {
        _db.Columns.Update(column);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Updated column {ColumnId}: {Title}", column.Id, column.Title);
        return column;
    }

    public async Task DeleteAsync(int id)
    {
        var col = await _db.Columns.FindAsync(id);
        if (col is not null)
        {
            _db.Columns.Remove(col);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted column {ColumnId}: {Title}", id, col.Title);
        }
        else
        {
            _logger.LogWarning("Column {ColumnId} not found for deletion", id);
        }
    }

    public async Task UpdateOrderAsync(int id, int newOrder)
    {
        var col = await _db.Columns.FindAsync(id);
        if (col is not null)
        {
            col.Order = newOrder;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Updated column {ColumnId} order to {NewOrder}", id, newOrder);
        }
        else
        {
            _logger.LogWarning("Column {ColumnId} not found for order update", id);
        }
    }
}
