using System.Net;
using DotaLane.AdviceService.Services;
using Serilog;
using Consul;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        Path.Combine(
            Environment.GetEnvironmentVariable("HOME") ?? "/tmp",
            "magnus", "DotaLane", "logs", "AdviceService", "log-.txt"),
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

    // baka: gRPC needs HTTP/2. Kestrel defaults to HTTP/1.1 + HTTPS,
    // baka: so we explicitly use HTTP/2 without TLS (h2c) for dev.
    // baka: Also listen on 5005 for HTTP/1.1 health checks.
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Listen(IPAddress.Any, 5003, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
        options.Listen(IPAddress.Any, 5005, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
    });

    builder.Services.AddGrpc();
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
            ID = $"advice-service-{Guid.NewGuid():N}",
            Name = "advice-service",
            Address = advertiseHost,
            Port = 5003,
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{advertiseHost}:5005/healthz",
                Interval = TimeSpan.FromSeconds(10),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30),
            },
        };

        await consul.Agent.ServiceRegister(registration);
        Log.Information("AdviceService registered with Consul as advice-service on port 5003");

        app.Lifetime.ApplicationStopping.Register(() =>
        {
            try { consul.Agent.ServiceDeregister(registration.ID).GetAwaiter().GetResult(); }
            catch { /* ignore */ }
            Log.Information("AdviceService deregistered from Consul");
        });
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Consul not available — skipping registration");
    }

    app.MapGrpcService<AdviceServiceImpl>();
    app.MapGet("/", () => "DotaLane Advice Service");

    Log.Information("AdviceService starting on port 5003 (HTTP/2), health on 5005 (HTTP/1.1)");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AdviceService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
