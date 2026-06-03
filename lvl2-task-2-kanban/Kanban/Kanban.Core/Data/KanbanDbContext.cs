using Microsoft.EntityFrameworkCore;
using Kanban.Core.Models;

namespace Kanban.Core.Data;

public class KanbanDbContext : DbContext
{
    public KanbanDbContext(DbContextOptions<KanbanDbContext> options) : base(options) { }

    public DbSet<Column> Columns => Set<Column>();
    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Card → Column: 1:N relationship
        modelBuilder.Entity<Card>(entity =>
        {
            // HasOne/WithMany — each Card belongs to one Column, each Column has many Cards
            entity.HasOne(c => c.Column)
                  .WithMany(col => col.Cards)
                  .HasForeignKey(c => c.ColumnId)  // explicit FK — convention matches, but avoids ambiguity
                  .OnDelete(DeleteBehavior.Cascade); // deleting a Column cascades to its Cards

            // Index on (ColumnId, Order) for faster queries — no unique constraint
            // to avoid conflicts during drag-drop reordering
            entity.HasIndex(c => new { c.ColumnId, c.Order });
        });
    }
}
