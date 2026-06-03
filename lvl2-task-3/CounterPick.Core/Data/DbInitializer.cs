using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CounterPick.Core.Models;

namespace CounterPick.Core.Data;

public class DbInitializer
{
    public static async Task Initialize(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        if (!await userManager.Users.AnyAsync())
            await SeedUsers(userManager);

        if (!await db.Heroes.AnyAsync())
        {
            var admin = await userManager.FindByNameAsync("admin");
            var adminId = admin?.Id ?? "";
            await SeedHeroes(db, adminId);
        }
    }

    private static async Task SeedHeroes(AppDbContext db, string adminId)
    {
        using var http = new HttpClient();
        HeroApiDto[]? heroes;

        try
        {
            heroes = await http.GetFromJsonAsync<HeroApiDto[]>(
                "https://api.opendota.com/api/heroStats");
        }
        catch
        {
            heroes = null;
        }

        if (heroes is null || heroes.Length == 0)
            return;

        var heroEntities = heroes.Select(h => new Hero
        {
            Id = h.Id,
            Name = h.Name.Replace("npc_dota_hero_", ""),
            LocalizedName = h.LocalizedName,
            PrimaryAttr = h.PrimaryAttr,
            AttackType = h.AttackType,
            Roles = string.Join(",", h.Roles),
            ImageUrl = $"https://cdn.steamstatic.com{h.Img}"
        }).ToList();

        db.Heroes.AddRange(heroEntities);
        await db.SaveChangesAsync();

        using var tx = await db.Database.BeginTransactionAsync();

        var heroList = await db.Heroes.ToListAsync();
        var rng = new Random();
        var suggestions = new List<CounterSuggestion>();

        foreach (var hero in heroList)
        {
            var others = heroList.Where(h => h.Id != hero.Id).OrderBy(_ => rng.Next()).Take(2).ToList();
            foreach (var counter in others)
            {
                suggestions.Add(new CounterSuggestion
                {
                    HeroId = hero.Id,
                    CounterHeroId = counter.Id,
                    Reason = "Reason will be added by users. Vote on the best one!",
                    SuggestedById = adminId
                });
            }
        }

        db.CounterSuggestions.AddRange(suggestions);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static async Task SeedUsers(UserManager<IdentityUser> userManager)
    {
        var admin = new IdentityUser
        {
            UserName = "admin",
            Email = "admin@counterpick.com"
        };
        await userManager.CreateAsync(admin, "Dota2@Secure2024!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    private class HeroApiDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("localized_name")] public string LocalizedName { get; set; } = "";
        [JsonPropertyName("primary_attr")] public string PrimaryAttr { get; set; } = "";
        [JsonPropertyName("attack_type")] public string AttackType { get; set; } = "";
        [JsonPropertyName("roles")] public string[] Roles { get; set; } = [];
        [JsonPropertyName("img")] public string Img { get; set; } = "";
    }
}
