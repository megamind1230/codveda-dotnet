using Microsoft.EntityFrameworkCore;
using FaceRank.Core.Models;

namespace FaceRank.Core.Data;

public class FaceRankDbContext : DbContext
{
    public FaceRankDbContext(DbContextOptions<FaceRankDbContext> options) : base(options) { }

    public DbSet<Person> People => Set<Person>();
    public DbSet<Vote> Votes => Set<Vote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasIndex(p => p.Gender);
            entity.HasIndex(p => p.EloRating);
        });

        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasOne(v => v.Winner).WithMany().HasForeignKey(v => v.WinnerId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(v => v.Loser).WithMany().HasForeignKey(v => v.LoserId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
