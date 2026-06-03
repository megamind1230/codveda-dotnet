using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Kanban.Core.Data;
using Kanban.Core.Models;

namespace Kanban.Core.Services;

public class CardService : ICardService
{
    private readonly KanbanDbContext _db;
    private readonly ILogger<CardService> _logger;
    public CardService(KanbanDbContext db, ILogger<CardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Card>> GetByColumnIdAsync(int columnId)
    {
        _logger.LogDebug("Getting cards for column {ColumnId}", columnId);
        return await _db.Cards.Where(c => c.ColumnId == columnId).OrderBy(c => c.Order).ToListAsync();
    }

    public async Task<Card?> GetByIdAsync(int id)
    {
        var card = await _db.Cards.Include(c => c.Column).FirstOrDefaultAsync(c => c.Id == id);
        if (card is null)
            _logger.LogWarning("Card {CardId} not found", id);
        else
            _logger.LogDebug("Retrieved card {CardId}: {Title}", id, card.Title);
        return card;
    }

    public async Task<Card> CreateAsync(Card card)
    {
        _db.Cards.Add(card);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Created card {CardId}: {Title}", card.Id, card.Title);
        return card;
    }

    public async Task<Card> UpdateAsync(Card card)
    {
        _db.Cards.Update(card);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Updated card {CardId}: {Title}", card.Id, card.Title);
        return card;
    }

    public async Task DeleteAsync(int id)
    {
        var card = await _db.Cards.FindAsync(id);
        if (card is not null)
        {
            _db.Cards.Remove(card);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted card {CardId}: {Title}", id, card.Title);
        }
        else
        {
            _logger.LogWarning("Card {CardId} not found for deletion", id);
        }
    }

    public async Task MoveCardAsync(int id, int targetColumnId, int newOrder)
    {
        var card = await _db.Cards.FindAsync(id);
        if (card is not null)
        {
            card.ColumnId = targetColumnId;
            card.Order = newOrder;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Moved card {CardId} to column {TargetColumnId} at order {NewOrder}", id, targetColumnId, newOrder);
        }
        else
        {
            _logger.LogWarning("Card {CardId} not found for move", id);
        }
    }
}
