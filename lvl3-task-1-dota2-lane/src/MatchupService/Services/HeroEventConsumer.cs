using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DotaLane.MatchupService.Services;

public class HeroEventConsumer : BackgroundService
{
    private readonly ILogger<HeroEventConsumer> _logger;
    private readonly string _rabbitMqHost;

    public HeroEventConsumer(ILogger<HeroEventConsumer> logger, string rabbitMqHost = "localhost")
    {
        _logger = logger;
        _rabbitMqHost = rabbitMqHost;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqHost,
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                DispatchConsumersAsync = true,
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(
                exchange: "dotalane.hero",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            var queueName = "matchup-service.hero-events";

            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            channel.QueueBind(
                queue: queueName,
                exchange: "dotalane.hero",
                routingKey: "hero.updated");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received RabbitMQ event: {Message}", message);
            };

            channel.BasicConsume(
                queue: queueName,
                autoAck: true,
                consumer: consumer);

            _logger.LogInformation("MatchupService subscribed to dotalane.hero -> {Queue}", queueName);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("MatchupService RabbitMQ consumer stopping");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ not available — consumer disabled");
        }
    }
}
