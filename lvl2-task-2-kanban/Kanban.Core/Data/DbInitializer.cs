using Microsoft.Extensions.Logging;
using Kanban.Core.Models;

namespace Kanban.Core.Data;

public static class DbInitializer
{
    public static void Seed(KanbanDbContext db, ILogger? logger = null)
    {
        if (db.Columns.Any())
        {
            logger?.LogDebug("Database already seeded, skipping");
            return;
        }

        var todo = new Column { Title = "To Do",       Order = 0 };
        var prog = new Column { Title = "In Progress", Order = 1 };
        var done = new Column { Title = "Done",        Order = 2 };

        db.Columns.AddRange(todo, prog, done);
        db.SaveChanges();

        db.Cards.AddRange(
            new Card { Title = "Set up project", Description = "Initialize the repo", Order = 0, ColumnId = todo.Id },
            new Card { Title = "Design mockup",  Description = "Figma wireframes",    Order = 1, ColumnId = todo.Id },
            new Card { Title = "Build API",      Description = "Implement endpoints", Order = 0, ColumnId = prog.Id },
            new Card { Title = "Add auth",       Description = "JWT tokens",          Order = 1, ColumnId = prog.Id },
            new Card { Title = "Deploy to prod", Description = "CI/CD pipeline",      Order = 0, ColumnId = done.Id },
            new Card { Title = "Write docs",     Description = "README + API docs",   Order = 1, ColumnId = done.Id }
        );
        db.SaveChanges();

        logger?.LogInformation("Seeded database with 3 columns and 6 cards");
    }
}
