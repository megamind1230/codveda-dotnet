using Microsoft.EntityFrameworkCore;
using Kanban.Core.Data;
using Kanban.Core.Services;
using Kanban.Core.Models;
using Kanban.Core.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<KanbanDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IColumnService, ColumnService>();
builder.Services.AddScoped<ICardService, CardService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestTiming();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

var cardApi = app.MapGroup("/api/cards");

cardApi.MapGet("/by-column/{columnId}", async (int columnId, ICardService service) =>
    Results.Ok(await service.GetByColumnIdAsync(columnId)));

cardApi.MapPost("/", async (Card card, ICardService service) =>
{
    var created = await service.CreateAsync(card);
    return Results.Created($"/api/cards/{created.Id}", created);
});

cardApi.MapPut("/{id}", async (int id, Card card, ICardService service) =>
{
    card.Id = id;
    return Results.Ok(await service.UpdateAsync(card));
});

cardApi.MapPatch("/{id}/move", async (int id, MoveCardRequest dto, ICardService service) =>
{
    await service.MoveCardAsync(id, dto.TargetColumnId, dto.NewOrder);
    return Results.Ok();
});

cardApi.MapDelete("/{id}", async (int id, ICardService service) =>
{
    await service.DeleteAsync(id);
    return Results.Ok();
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KanbanDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    db.Database.EnsureCreated();
    DbInitializer.Seed(db, logger);
}

app.Run();
