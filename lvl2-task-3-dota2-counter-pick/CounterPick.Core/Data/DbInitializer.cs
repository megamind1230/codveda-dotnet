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
        else
        {
            await UpdateExistingReasons(db);
        }
    }

    private static async Task UpdateExistingReasons(AppDbContext db)
    {
        var heroes = await db.Heroes.ToListAsync();
        var heroMap = heroes.ToDictionary(h => h.Id);
        var stale = await db.CounterSuggestions
            .Where(cs => cs.Reason == "" || cs.Reason == "Reason will be added by users. Vote on the best one!")
            .ToListAsync();
        if (stale.Count == 0)
            return;
        foreach (var cs in stale)
        {
            if (heroMap.TryGetValue(cs.HeroId, out var hero) &&
                heroMap.TryGetValue(cs.CounterHeroId, out var counter))
            {
                cs.Reason = GenerateReason(hero, counter);
            }
        }
        await db.SaveChangesAsync();
    }

    private static string GenerateReason(Hero hero, Hero counter)
    {
        var heroRoles = hero.Roles.Split(',', StringSplitOptions.TrimEntries);
        var counterRoles = counter.Roles.Split(',', StringSplitOptions.TrimEntries);

        var reasons = new List<string>();

        if (counter.AttackType == "Ranged" && hero.AttackType == "Melee")
            reasons.Add($"{counter.LocalizedName} can kite {hero.LocalizedName} from range, forcing them to burn spells or items just to close the gap.");

        if (hero.AttackType == "Ranged" && counter.AttackType == "Melee")
            reasons.Add($"{counter.LocalizedName} has gap-closing abilities that punish {hero.LocalizedName}'s ranged positioning and force them into melee range.");

        if (counter.PrimaryAttr == "int" && hero.PrimaryAttr == "agi")
            reasons.Add($"{counter.LocalizedName}'s intelligence-based spells and silences cripple {hero.LocalizedName}'s mobility and attack-dependent playstyle.");

        if (counter.PrimaryAttr == "agi" && hero.PrimaryAttr == "int")
            reasons.Add($"{counter.LocalizedName} can close the distance and out-damage {hero.LocalizedName} before their spells come off cooldown.");

        if (counter.PrimaryAttr == "str" && hero.PrimaryAttr == "agi")
            reasons.Add($"{counter.LocalizedName}'s strength-based bulk and disables prevent {hero.LocalizedName} from kiting effectively.");

        if (counter.PrimaryAttr == "agi" && hero.PrimaryAttr == "str")
            reasons.Add($"{counter.LocalizedName} burns through {hero.LocalizedName}'s health pool faster than they can sustain through strength regeneration.");

        if (counterRoles.Intersect(new[] { "Nuker", "Disabler", "Initiator" }).Any() &&
            heroRoles.Contains("Carry"))
            reasons.Add($"{counter.LocalizedName} can burst down {hero.LocalizedName} early and often, delaying their farm and power spikes.");

        if (counterRoles.Contains("Carry") && heroRoles.Contains("Support"))
            reasons.Add($"{counter.LocalizedName} scales harder than {hero.LocalizedName}, becoming unmanageable in the mid-to-late game.");

        if (counterRoles.Contains("Support") && heroRoles.Contains("Carry"))
            reasons.Add($"{counter.LocalizedName} can zone out {hero.LocalizedName} in lane with cheap harass and save abilities.");

        if (counterRoles.Contains("Durable") && heroRoles.Contains("Nuker"))
            reasons.Add($"{counter.LocalizedName}'s durability lets them shrug off {hero.LocalizedName}'s burst combo and turn the fight around.");

        if (counterRoles.Contains("Escape") && heroRoles.Contains("Disabler"))
            reasons.Add($"{counter.LocalizedName}'s mobility tools allow them to evade {hero.LocalizedName}'s initiation and waste their cooldowns.");

        if (heroRoles.Contains("Initiator") && counterRoles.Contains("Disabler"))
            reasons.Add($"{counter.LocalizedName} can interrupt {hero.LocalizedName}'s initiation with instant crowd control, leaving them stranded.");

        if (reasons.Count > 0)
            return reasons[Random.Shared.Next(reasons.Count)];

        return $"{counter.LocalizedName} has the tools to consistently outplay {hero.LocalizedName} across all stages of the game.";
    }

    //#baka fetches 127 heroes from OpenDota API, maps them, seeds 2 random counter suggestions per hero with admin as author
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

        //#baka strip "npc_dota_hero_" prefix from OpenDota name; join Roles array to comma-string; build full Steam CDN URL
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

        //#baka transaction: if suggestion seeding fails, heroes roll back too — prevents orphaned heroes with no suggestions
        using var tx = await db.Database.BeginTransactionAsync();

        var heroList = await db.Heroes.ToListAsync();
        var rng = new Random();
        var suggestions = new List<CounterSuggestion>();

        foreach (var hero in heroList)
        {
            //#baka pick 2 random OTHER heroes (excluding self) for each hero's counter suggestions
            var others = heroList.Where(h => h.Id != hero.Id).OrderBy(_ => rng.Next()).Take(2).ToList();
            foreach (var counter in others)
            {
                suggestions.Add(new CounterSuggestion
                {
                    HeroId = hero.Id,
                    CounterHeroId = counter.Id,
                    Reason = GenerateReason(hero, counter),
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
