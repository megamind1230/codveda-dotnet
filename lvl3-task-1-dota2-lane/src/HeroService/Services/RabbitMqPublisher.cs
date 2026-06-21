using RabbitMQ.Client;
using DotaLane.HeroService.Models;

namespace DotaLane.HeroService.Services;

public class RabbitMqPublisher : IDisposable
{
    private readonly string _exchangeName;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _available;

    public RabbitMqPublisher(ILogger<RabbitMqPublisher> logger, string rabbitMqHost = "localhost")
    {
        _logger = logger;
        _exchangeName = "dotalane.hero";

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = rabbitMqHost,
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                DispatchConsumersAsync = true,
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _available = true;
            _logger.LogInformation("RabbitMQ publisher connected, exchange {Exchange} declared", _exchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ not available — publisher disabled");
            _available = false;
        }
    }

    public Task PublishHeroStatsUpdatedAsync(List<int> heroIds)
    {
        if (!_available)
        {
            _logger.LogWarning("RabbitMQ not available — skipping publish");
            return Task.CompletedTask;
        }

        var evt = new HeroStatsUpdatedEvent
        {
            HeroIds = heroIds,
        };

        var body = System.Text.Encoding.UTF8.GetBytes(evt.ToJson());

        _channel!.BasicPublish(
            exchange: _exchangeName,
            routingKey: "hero.updated",
            body: body);

        _logger.LogInformation("Published HeroStatsUpdated event for {Count} hero(s): {Ids}",
            heroIds.Count, string.Join(", ", heroIds));

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
