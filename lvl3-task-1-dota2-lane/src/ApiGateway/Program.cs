using System.Net;
using Serilog;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Consul;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        Path.Combine(
            Environment.GetEnvironmentVariable("HOME") ?? "/tmp",
            "magnus", "DotaLane", "logs", "ApiGateway", "log-.txt"),
        rollingInterval: RollingInterval.Day)
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var consulAddr = Environment.GetEnvironmentVariable("CONSUL_HTTP_ADDR") ?? "http://localhost:8500";
    var advertiseHost = Environment.GetEnvironmentVariable("ADVERTISE_HOST") ?? "localhost";

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Listen(IPAddress.Any, 5000);
    });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });
    });

    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddJsonFile("ocelot.Development.json", optional: false, reloadOnChange: true);
    }
    else
    {
        builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
        builder.Configuration.AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
    }
    builder.Services.AddOcelot().AddConsul();
    builder.Services.AddSingleton<IConsulClient>(_ =>
        new ConsulClient(c => c.Address = new Uri(consulAddr)));

    var app = builder.Build();

    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/healthz")
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("healthy");
            return;
        }
        await next();
    });

    try
    {
        var consul = app.Services.GetRequiredService<IConsulClient>();
        var registration = new AgentServiceRegistration
        {
            ID = $"api-gateway-{Guid.NewGuid():N}",
            Name = "api-gateway",
            Address = advertiseHost,
            Port = 5000,
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{advertiseHost}:5000/healthz",
                Interval = TimeSpan.FromSeconds(10),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30),
            },
        };

        await consul.Agent.ServiceRegister(registration);
        Log.Information("ApiGateway registered with Consul as api-gateway on port 5000");

        app.Lifetime.ApplicationStopping.Register(() =>
        {
            try { consul.Agent.ServiceDeregister(registration.ID).GetAwaiter().GetResult(); }
            catch { /* ignore */ }
            Log.Information("ApiGateway deregistered from Consul");
        });
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Consul not available — skipping registration");
    }

    app.UseCors();
    app.UseSerilogRequestLogging();
    await app.UseOcelot();

    Log.Information("ApiGateway starting on port 5000");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ApiGateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
