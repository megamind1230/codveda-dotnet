using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CounterPick.Core.Models;

namespace CounterPick.Core.Data;

public class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Hero> Heroes => Set<Hero>();
    public DbSet<CounterSuggestion> CounterSuggestions => Set<CounterSuggestion>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Hero>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Name).HasMaxLength(100);
            e.Property(h => h.LocalizedName).HasMaxLength(100);
        });

        builder.Entity<CounterSuggestion>(e =>
        {
            e.HasKey(cs => cs.Id);
            e.HasOne(cs => cs.Hero)
                .WithMany(h => h.CounterSuggestions)
                .HasForeignKey(cs => cs.HeroId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(cs => cs.CounterHero)
                .WithMany()
                .HasForeignKey(cs => cs.CounterHeroId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.HasIndex(rt => rt.Token).IsUnique();
        });
    }
}
