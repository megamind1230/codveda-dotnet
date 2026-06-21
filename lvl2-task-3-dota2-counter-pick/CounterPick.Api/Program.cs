using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CounterPick.Api.Authorization;
using CounterPick.Auth.Services;
using CounterPick.Core.Constants;
using CounterPick.Core.Data;

var builder = WebApplication.CreateBuilder(args);

// --- Identity + Roles ---
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// --- JWT Authentication ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not set");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CounterPick",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CounterPickApi",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
})
.AddGoogleIfConfigured(builder.Configuration)
.AddMicrosoftAccountIfConfigured(builder.Configuration);

// --- Authorization Policies ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.OwnsSuggestion, policy =>
        policy.Requirements.Add(new CounterSuggestionOwnerRequirement()));
});

// --- DB ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IAuthorizationHandler, CounterSuggestionOwnerHandler>();

// --- CORS ---
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddControllers();

var app = builder.Build();

// --- Seed ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await db.Database.EnsureCreatedAsync();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    await DbInitializer.Initialize(db, userManager);
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static class AuthExtensions
{
    public static AuthenticationBuilder AddGoogleIfConfigured(
        this AuthenticationBuilder builder, IConfiguration config)
    {
        //#baka only registers Google handler if both ClientId+ClientSecret set in user-secrets; no-op otherwise
        var clientId = config["Google:ClientId"];
        var clientSecret = config["Google:ClientSecret"];
        if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
        {
            builder.AddGoogle(options =>
            {
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.SaveTokens = true; //#baka keeps the OAuth tokens so server can use them later
                options.ClaimActions.MapJsonKey("picture", "picture"); //#baka maps Google's "picture" field to a claim so the app can use it as user avatar
            });
        }
        return builder;
    }

    public static AuthenticationBuilder AddMicrosoftAccountIfConfigured(
        this AuthenticationBuilder builder, IConfiguration config)
    {
        //#baka same pattern as Google — skipped unless secrets exist in config
        var clientId = config["Microsoft:ClientId"];
        var clientSecret = config["Microsoft:ClientSecret"];
        if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
        {
            builder.AddMicrosoftAccount(options =>
            {
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.Scope.Add("User.Read");
                options.SaveTokens = true;
                //#baka MS returns "mail" instead of "email" — this maps it to the standard ClaimTypes.Email
                options.ClaimActions.MapJsonKey("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", "mail");
                options.ClaimActions.MapJsonKey("picture", "picture");
            });
        }
        return builder;
    }
}
