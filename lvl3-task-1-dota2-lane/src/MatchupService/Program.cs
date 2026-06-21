using System.Net;
using DotaLane.MatchupService.Services;
using DotaLane.AdviceService;
using Grpc.Net.Client;
using Serilog;
using Consul;
using AdviceServiceClient = DotaLane.AdviceService.AdviceService.AdviceServiceClient;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        Path.Combine(
            Environment.GetEnvironmentVariable("HOME") ?? "/tmp",
            "magnus", "DotaLane", "logs", "MatchupService", "log-.txt"),
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
    var heroServiceUrl = Environment.GetEnvironmentVariable("HERO_SERVICE_URL") ?? "http://localhost:5001";
    var adviceServiceUrl = Environment.GetEnvironmentVariable("ADVICE_SERVICE_URL") ?? "http://localhost:5003";

    // baka: port 5002 = HTTP/2 for gRPC clients.
    // baka: port 5004 = HTTP/1.1 for REST (API Gateway proxy).
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Listen(IPAddress.Any, 5002,
            o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
        options.Listen(IPAddress.Any, 5004,
            o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
    });

    // baka: shared HTTP client for HeroService REST calls.
    builder.Services.AddHttpClient("HeroService", client =>
    {
        client.BaseAddress = new Uri(heroServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(5);
    });

    // baka: gRPC channel + client for AdviceService.
    builder.Services.AddSingleton<AdviceServiceClient>(_ =>
    {
        var channel = GrpcChannel.ForAddress(adviceServiceUrl);
        return new AdviceServiceClient(channel);
    });

    builder.Services.AddGrpc();
    builder.Services.AddControllers();
    builder.Services.AddHostedService(sp =>
        ActivatorUtilities.CreateInstance<HeroEventConsumer>(sp, rabbitMqHost));
    builder.Services.AddSingleton<IConsulClient>(_ =>
        new ConsulClient(c => c.Address = new Uri(consulAddr)));

    var app = builder.Build();

    app.MapGet("/healthz", () => Results.Ok("healthy"));

    try
    {
        var consul = app.Services.GetRequiredService<IConsulClient>();
        var registration = new AgentServiceRegistration
        {
            ID = $"matchup-service-{Guid.NewGuid():N}",
            Name = "matchup-service",
            Address = advertiseHost,
            Port = 5004,
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{advertiseHost}:5004/healthz",
                Interval = TimeSpan.FromSeconds(10),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30),
            },
        };

        await consul.Agent.ServiceRegister(registration);
        Log.Information("MatchupService registered with Consul as matchup-service on port 5004");

        app.Lifetime.ApplicationStopping.Register(() =>
        {
            try { consul.Agent.ServiceDeregister(registration.ID).GetAwaiter().GetResult(); }
            catch { /* ignore */ }
            Log.Information("MatchupService deregistered from Consul");
        });
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Consul not available — skipping registration");
    }

    app.MapGrpcService<MatchupServiceImpl>();
    app.MapControllers();

    Log.Information("MatchupService: gRPC on 5002, REST on 5004");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MatchupService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
