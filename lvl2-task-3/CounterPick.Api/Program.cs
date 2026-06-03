using System.Text;
using System.Threading.RateLimiting;
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
    options.AddPolicy(AppPolicies.CanComment, policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy(AppPolicies.CanDeleteAnyComment, policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy(AppPolicies.VerifiedAccount, policy =>
        policy.RequireClaim("ExternalLogin", "true"));

    options.AddPolicy(AppPolicies.OwnsComment, policy =>
        policy.Requirements.Add(new CommentOwnerRequirement()));
});

// --- Rate Limiting ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("SearchLimit", context =>
    {
        var isAuth = context.User.Identity?.IsAuthenticated == true;
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: isAuth
                ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "auth"
                : context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window = isAuth ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(10),
                PermitLimit = isAuth ? 100 : 3,
                SegmentsPerWindow = 1
            });
    });
    options.AddPolicy("LoginLimit", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 5,
                SegmentsPerWindow = 1
            }));
});

// --- DB ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IAuthorizationHandler, CommentOwnerHandler>();

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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static class AuthExtensions
{
    public static AuthenticationBuilder AddGoogleIfConfigured(
        this AuthenticationBuilder builder, IConfiguration config)
    {
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
                options.SaveTokens = true;
                options.ClaimActions.MapJsonKey("picture", "picture");
            });
        }
        return builder;
    }

    public static AuthenticationBuilder AddMicrosoftAccountIfConfigured(
        this AuthenticationBuilder builder, IConfiguration config)
    {
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
                options.ClaimActions.MapJsonKey("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", "mail");
                options.ClaimActions.MapJsonKey("picture", "picture");
            });
        }
        return builder;
    }
}
