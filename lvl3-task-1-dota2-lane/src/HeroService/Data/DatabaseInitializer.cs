using Dapper;
using Microsoft.Data.Sqlite;
using DotaLane.HeroService.Models;

namespace DotaLane.HeroService.Data;

// baka: creates heroes.db on disk if missing, seeds 30 heroes.
// baka: no migrations — drops and recreates are manual.
public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Initialize()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS Heroes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                PrimaryAttribute TEXT NOT NULL,
                DamageMin INTEGER NOT NULL,
                DamageMax INTEGER NOT NULL,
                AttackRange INTEGER NOT NULL,
                BaseAttackTime REAL NOT NULL,
                Armor REAL NOT NULL,
                MoveSpeed INTEGER NOT NULL,
                DayVision INTEGER NOT NULL,
                NightVision INTEGER NOT NULL,
                StrGain REAL NOT NULL,
                AgiGain REAL NOT NULL,
                IntGain REAL NOT NULL,
                BaseStr INTEGER NOT NULL,
                BaseAgi INTEGER NOT NULL,
                BaseInt INTEGER NOT NULL,
                Lane TEXT NOT NULL
            );
        """);

        var count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Heroes");
        if (count == 0)
        {
            Seed(conn);
        }
    }

    private static void Seed(SqliteConnection conn)
    {
        var heroes = GetSeedData();
        conn.Execute("""
            INSERT INTO Heroes (Name, PrimaryAttribute, DamageMin, DamageMax,
                AttackRange,                 BaseAttackTime, Armor, MoveSpeed, DayVision, NightVision,
                StrGain, AgiGain, IntGain, BaseStr, BaseAgi, BaseInt, Lane)
            VALUES (@Name, @PrimaryAttribute, @DamageMin, @DamageMax,
                @AttackRange, @BaseAttackTime, @Armor, @MoveSpeed, @DayVision, @NightVision,
                @StrGain, @AgiGain, @IntGain, @BaseStr, @BaseAgi, @BaseInt, @Lane)
        """, heroes);
    }

    // baka: stats approximate from Dota 2 wikis — close enough for
    // baka: meaningful comparisons, not guaranteed current patch
    private static IEnumerable<Hero> GetSeedData()
    {
        return new List<Hero>
        {
            // ===== MID =====
            new() { Name = "Shadow Fiend", PrimaryAttribute = "agi", DamageMin = 35, DamageMax = 41, AttackRange = 500, BaseAttackTime = 1.7, Armor = 2.0, MoveSpeed = 310, DayVision = 1800, NightVision = 800, StrGain = 2.5, AgiGain = 3.5, IntGain = 1.7, BaseStr = 19, BaseAgi = 21, BaseInt = 16, Lane = "mid" },
            new() { Name = "Lina", PrimaryAttribute = "int", DamageMin = 49, DamageMax = 57, AttackRange = 670, BaseAttackTime = 1.6, Armor = 1.5, MoveSpeed = 310, DayVision = 1800, NightVision = 800, StrGain = 2.0, AgiGain = 1.5, IntGain = 3.7, BaseStr = 18, BaseAgi = 16, BaseInt = 27, Lane = "mid" },
            new() { Name = "Invoker", PrimaryAttribute = "int", DamageMin = 43, DamageMax = 49, AttackRange = 600, BaseAttackTime = 1.7, Armor = 1.2, MoveSpeed = 290, DayVision = 1800, NightVision = 800, StrGain = 2.1, AgiGain = 1.9, IntGain = 3.2, BaseStr = 19, BaseAgi = 16, BaseInt = 24, Lane = "mid" },
            new() { Name = "Sniper", PrimaryAttribute = "agi", DamageMin = 40, DamageMax = 46, AttackRange = 950, BaseAttackTime = 1.7, Armor = 1.5, MoveSpeed = 300, DayVision = 1800, NightVision = 800, StrGain = 1.8, AgiGain = 2.5, IntGain = 2.0, BaseStr = 17, BaseAgi = 19, BaseInt = 18, Lane = "mid" },
            new() { Name = "Puck", PrimaryAttribute = "int", DamageMin = 47, DamageMax = 56, AttackRange = 550, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 305, DayVision = 1800, NightVision = 800, StrGain = 2.0, AgiGain = 1.7, IntGain = 3.0, BaseStr = 17, BaseAgi = 18, BaseInt = 23, Lane = "mid" },
            new() { Name = "Ember Spirit", PrimaryAttribute = "agi", DamageMin = 52, DamageMax = 56, AttackRange = 150, BaseAttackTime = 1.6, Armor = 2.5, MoveSpeed = 310, DayVision = 1800, NightVision = 800, StrGain = 2.1, AgiGain = 2.6, IntGain = 1.8, BaseStr = 20, BaseAgi = 22, BaseInt = 18, Lane = "mid" },
            new() { Name = "Storm Spirit", PrimaryAttribute = "int", DamageMin = 47, DamageMax = 57, AttackRange = 480, BaseAttackTime = 1.7, Armor = 2.0, MoveSpeed = 290, DayVision = 1800, NightVision = 800, StrGain = 2.0, AgiGain = 1.5, IntGain = 3.2, BaseStr = 17, BaseAgi = 17, BaseInt = 24, Lane = "mid" },
            new() { Name = "Void Spirit", PrimaryAttribute = "int", DamageMin = 51, DamageMax = 57, AttackRange = 450, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 305, DayVision = 1800, NightVision = 800, StrGain = 2.0, AgiGain = 1.8, IntGain = 2.8, BaseStr = 17, BaseAgi = 18, BaseInt = 22, Lane = "mid" },
            new() { Name = "Queen of Pain", PrimaryAttribute = "int", DamageMin = 48, DamageMax = 56, AttackRange = 550, BaseAttackTime = 1.6, Armor = 2.0, MoveSpeed = 300, DayVision = 1800, NightVision = 800, StrGain = 2.0, AgiGain = 1.7, IntGain = 3.2, BaseStr = 18, BaseAgi = 18, BaseInt = 24, Lane = "mid" },
            new() { Name = "Zeus", PrimaryAttribute = "int", DamageMin = 42, DamageMax = 48, AttackRange = 380, BaseAttackTime = 1.7, Armor = 1.5, MoveSpeed = 295, DayVision = 1800, NightVision = 800, StrGain = 1.8, AgiGain = 1.1, IntGain = 3.3, BaseStr = 17, BaseAgi = 12, BaseInt = 22, Lane = "mid" },

            // ===== SAFE LANE =====
            new() { Name = "Juggernaut", PrimaryAttribute = "agi", DamageMin = 50, DamageMax = 54, AttackRange = 150, BaseAttackTime = 1.6, Armor = 2.5, MoveSpeed = 310, DayVision = 1800, NightVision = 800, StrGain = 2.2, AgiGain = 2.8, IntGain = 1.4, BaseStr = 20, BaseAgi = 26, BaseInt = 14, Lane = "safe" },
            new() { Name = "Phantom Assassin", PrimaryAttribute = "agi", DamageMin = 49, DamageMax = 51, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 310, DayVision = 1800, NightVision = 800, StrGain = 2.0, AgiGain = 3.2, IntGain = 1.0, BaseStr = 18, BaseAgi = 23, BaseInt = 13, Lane = "safe" },
            new() { Name = "Faceless Void", PrimaryAttribute = "agi", DamageMin = 52, DamageMax = 58, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 300, DayVision = 1800, NightVision = 800, StrGain = 2.2, AgiGain = 2.7, IntGain = 1.5, BaseStr = 18, BaseAgi = 19, BaseInt = 15, Lane = "safe" },
            new() { Name = "Spectre", PrimaryAttribute = "agi", DamageMin = 44, DamageMax = 48, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 290, DayVision = 1800, NightVision = 800, StrGain = 2.5, AgiGain = 2.2, IntGain = 1.6, BaseStr = 21, BaseAgi = 21, BaseInt = 16, Lane = "safe" },
            new() { Name = "Anti-Mage", PrimaryAttribute = "agi", DamageMin = 48, DamageMax = 52, AttackRange = 150, BaseAttackTime = 1.5, Armor = 2.5, MoveSpeed = 315, DayVision = 1800, NightVision = 800, StrGain = 1.6, AgiGain = 3.0, IntGain = 1.0, BaseStr = 18, BaseAgi = 22, BaseInt = 12, Lane = "safe" },
            new() { Name = "Sven", PrimaryAttribute = "str", DamageMin = 51, DamageMax = 55, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 315, DayVision = 1800, NightVision = 800, StrGain = 3.0, AgiGain = 1.8, IntGain = 1.3, BaseStr = 24, BaseAgi = 15, BaseInt = 13, Lane = "safe" },
            new() { Name = "Wraith King", PrimaryAttribute = "str", DamageMin = 50, DamageMax = 58, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.0, MoveSpeed = 315, DayVision = 1800, NightVision = 800, StrGain = 3.2, AgiGain = 1.5, IntGain = 1.3, BaseStr = 24, BaseAgi = 12, BaseInt = 14, Lane = "safe" },
            new() { Name = "Monkey King", PrimaryAttribute = "agi", DamageMin = 48, DamageMax = 52, AttackRange = 300, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 305, DayVision = 1800, NightVision = 800, StrGain = 2.2, AgiGain = 2.9, IntGain = 1.6, BaseStr = 19, BaseAgi = 22, BaseInt = 17, Lane = "safe" },
            new() { Name = "Drow Ranger", PrimaryAttribute = "agi", DamageMin = 44, DamageMax = 50, AttackRange = 625, BaseAttackTime = 1.7, Armor = 1.5, MoveSpeed = 305, DayVision = 1800, NightVision = 1000, StrGain = 1.9, AgiGain = 3.0, IntGain = 1.4, BaseStr = 16, BaseAgi = 22, BaseInt = 15, Lane = "safe" },
            new() { Name = "Morphling", PrimaryAttribute = "agi", DamageMin = 43, DamageMax = 52, AttackRange = 350, BaseAttackTime = 1.6, Armor = 2.0, MoveSpeed = 310, DayVision = 1800, NightVision = 800, StrGain = 2.0, AgiGain = 3.5, IntGain = 1.5, BaseStr = 17, BaseAgi = 24, BaseInt = 15, Lane = "safe" },

            // ===== OFF LANE =====
            new() { Name = "Bristleback", PrimaryAttribute = "str", DamageMin = 49, DamageMax = 55, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 310, DayVision = 1800, NightVision = 800, StrGain = 3.2, AgiGain = 1.7, IntGain = 1.8, BaseStr = 24, BaseAgi = 14, BaseInt = 15, Lane = "off" },
            new() { Name = "Tidehunter", PrimaryAttribute = "str", DamageMin = 44, DamageMax = 50, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.0, MoveSpeed = 305, DayVision = 1800, NightVision = 800, StrGain = 3.3, AgiGain = 1.5, IntGain = 1.8, BaseStr = 26, BaseAgi = 13, BaseInt = 16, Lane = "off" },
            new() { Name = "Axe", PrimaryAttribute = "str", DamageMin = 52, DamageMax = 56, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 315, DayVision = 1800, NightVision = 800, StrGain = 3.0, AgiGain = 1.5, IntGain = 1.3, BaseStr = 25, BaseAgi = 18, BaseInt = 14, Lane = "off" },
            new() { Name = "Centaur Warrunner", PrimaryAttribute = "str", DamageMin = 49, DamageMax = 55, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 305, DayVision = 1800, NightVision = 800, StrGain = 3.8, AgiGain = 1.5, IntGain = 1.5, BaseStr = 26, BaseAgi = 14, BaseInt = 15, Lane = "off" },
            new() { Name = "Mars", PrimaryAttribute = "str", DamageMin = 50, DamageMax = 56, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 310, DayVision = 1800, NightVision = 800, StrGain = 3.2, AgiGain = 1.5, IntGain = 1.6, BaseStr = 24, BaseAgi = 14, BaseInt = 16, Lane = "off" },
            new() { Name = "Underlord", PrimaryAttribute = "str", DamageMin = 42, DamageMax = 48, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 300, DayVision = 1800, NightVision = 800, StrGain = 3.0, AgiGain = 1.4, IntGain = 2.0, BaseStr = 25, BaseAgi = 12, BaseInt = 18, Lane = "off" },
            new() { Name = "Timbersaw", PrimaryAttribute = "str", DamageMin = 49, DamageMax = 53, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 305, DayVision = 1800, NightVision = 800, StrGain = 2.8, AgiGain = 1.6, IntGain = 2.4, BaseStr = 22, BaseAgi = 14, BaseInt = 20, Lane = "off" },
            new() { Name = "Sand King", PrimaryAttribute = "str", DamageMin = 42, DamageMax = 52, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.0, MoveSpeed = 300, DayVision = 1800, NightVision = 800, StrGain = 2.9, AgiGain = 1.8, IntGain = 1.6, BaseStr = 22, BaseAgi = 14, BaseInt = 15, Lane = "off" },
            new() { Name = "Night Stalker", PrimaryAttribute = "str", DamageMin = 58, DamageMax = 62, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 310, DayVision = 1800, NightVision = 1200, StrGain = 3.0, AgiGain = 1.6, IntGain = 1.6, BaseStr = 22, BaseAgi = 15, BaseInt = 17, Lane = "off" },
            new() { Name = "Beastmaster", PrimaryAttribute = "str", DamageMin = 44, DamageMax = 48, AttackRange = 150, BaseAttackTime = 1.7, Armor = 2.5, MoveSpeed = 310, DayVision = 1800, NightVision = 800, StrGain = 2.9, AgiGain = 1.6, IntGain = 1.7, BaseStr = 23, BaseAgi = 15, BaseInt = 16, Lane = "off" },
        };
    }
}
