using Microsoft.EntityFrameworkCore;
using FaceRank.Core.Data;
using FaceRank.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<BlobStorageService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<FaceRankDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultDb")
        ?? "Data Source=FaceRank.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FaceRankDbContext>();
    db.Database.EnsureCreated();
    if (args.Contains("--seed"))
        await DbSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapGet("/vote", () => Results.Redirect("/vote/Male"));
app.MapGet("/leaderboard", () => Results.Redirect("/leaderboard/Male"));

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
