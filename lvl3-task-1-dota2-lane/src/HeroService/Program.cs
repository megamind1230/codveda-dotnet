using System.Net;
using Serilog;
using Consul;
using DotaLane.HeroService.Data;
using DotaLane.HeroService.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        Path.Combine(
            Environment.GetEnvironmentVariable("HOME") ?? "/tmp",
            "magnus", "DotaLane", "logs", "HeroService", "log-.txt"),
        rollingInterval: RollingInterval.Day)
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var consulAddr = Environment.GetEnvironmentVariable("CONSUL_HTTP_ADDR") ?? "http://localhost:8500";
    var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
    var advertiseHost = Environment.GetEnvironmentVariable("ADVERTISE_HOST") ?? "localhost";

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Listen(IPAddress.Any, 5001);
    });

    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=heroes.db";

    var dbInit = new DatabaseInitializer(connectionString);
    dbInit.Initialize();
    Log.Information("HeroService database initialized");

    builder.Services.AddSingleton(new HeroRepository(connectionString));
    builder.Services.AddControllers();
    builder.Services.AddSingleton<IConsulClient>(_ =>
        new ConsulClient(c => c.Address = new Uri(consulAddr)));
    builder.Services.AddSingleton<RabbitMqPublisher>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<RabbitMqPublisher>>();
        return new RabbitMqPublisher(logger, rabbitMqHost);
    });

    var app = builder.Build();

    app.MapGet("/healthz", () => Results.Ok("healthy"));

    try
    {
        var consul = app.Services.GetRequiredService<IConsulClient>();
        var registration = new AgentServiceRegistration
        {
            ID = $"hero-service-{Guid.NewGuid():N}",
            Name = "hero-service",
            Address = advertiseHost,
            Port = 5001,
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{advertiseHost}:5001/healthz",
                Interval = TimeSpan.FromSeconds(10),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30),
            },
        };

        await consul.Agent.ServiceRegister(registration);
        Log.Information("HeroService registered with Consul as hero-service on port 5001");

        app.Lifetime.ApplicationStopping.Register(() =>
        {
            try { consul.Agent.ServiceDeregister(registration.ID).GetAwaiter().GetResult(); }
            catch { /* ignore */ }
            Log.Information("HeroService deregistered from Consul");
        });
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Consul not available — skipping registration");
    }

    app.UseAuthorization();
    app.MapControllers();

    Log.Information("HeroService starting");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "HeroService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
