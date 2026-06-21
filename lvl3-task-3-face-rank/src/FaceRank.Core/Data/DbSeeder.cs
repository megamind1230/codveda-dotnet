using Microsoft.EntityFrameworkCore;
using FaceRank.Core.Models;

namespace FaceRank.Core.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(FaceRankDbContext db)
    {
        if (db.People.Any()) return;

        var people = new List<Person>();

        for (int i = 1; i <= 10; i++)
            people.Add(new Person
            {
                Name = MaleNames[i - 1],
                Gender = "Male",
                AvatarUrl = $"/uploads/male-{i}.jpg",
                EloRating = Random.Shared.Next(1300, 1600),
                VotesCount = 0
            });

        for (int i = 1; i <= 10; i++)
            people.Add(new Person
            {
                Name = FemaleNames[i - 1],
                Gender = "Female",
                AvatarUrl = $"/uploads/female-{i}.jpg",
                EloRating = Random.Shared.Next(1300, 1600),
                VotesCount = 0
            });

        db.People.AddRange(people);
        await db.SaveChangesAsync();

        var all = await db.People.ToListAsync();
        for (int i = 0; i < 40; i++)
        {
            var (a, b) = (Random.Shared.Next(all.Count), Random.Shared.Next(all.Count));
            if (a == b) continue;
            var winner = all[a];
            var loser = all[b];
            var (wNew, lNew) = Services.EloService.Calculate(winner.EloRating, loser.EloRating);
            winner.EloRating = wNew;
            loser.EloRating = lNew;
            winner.VotesCount++;
            loser.VotesCount++;
            db.Votes.Add(new Vote { WinnerId = winner.Id, LoserId = loser.Id });
        }
        await db.SaveChangesAsync();
    }

    private static readonly string[] MaleNames =
    [
        "Leonardo DiCaprio", "Brad Pitt", "Tom Hardy", "Idris Elba", "Ryan Gosling",
        "Chris Hemsworth", "Michael B. Jordan", "Timothée Chalamet", "Henry Cavill", "Keanu Reeves"
    ];

    private static readonly string[] FemaleNames =
    [
        "Scarlett Johansson", "Margot Robbie", "Zendaya", "Emma Stone", "Priyanka Chopra",
        "Gal Gadot", "Zoe Saldaña", "Ana de Armas", "Lupita Nyong'o", "Hailee Steinfeld"
    ];
}
