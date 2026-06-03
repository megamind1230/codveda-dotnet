using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CounterPick.Core.Models;

namespace CounterPick.Core.Data;

public class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Hero> Heroes => Set<Hero>();
    public DbSet<CounterSuggestion> CounterSuggestions => Set<CounterSuggestion>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<SuggestionLike> SuggestionLikes => Set<SuggestionLike>();
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

        builder.Entity<Comment>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.CounterSuggestion)
                .WithMany(cs => cs.Comments)
                .HasForeignKey(c => c.CounterSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.HasIndex(rt => rt.Token).IsUnique();
        });

        builder.Entity<SuggestionLike>(e =>
        {
            e.HasKey(sl => sl.Id);
            e.HasOne(sl => sl.Comment)
                .WithMany(c => c.Likes)
                .HasForeignKey(sl => sl.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(sl => new { sl.CommentId, sl.UserId }).IsUnique();
        });
    }
}
