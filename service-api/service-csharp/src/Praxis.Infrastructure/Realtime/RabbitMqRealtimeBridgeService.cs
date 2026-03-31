using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Praxis.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Praxis.Infrastructure.Realtime;

public sealed class RabbitMqRealtimeBridgeService(
    IHubContext<NotificationsHub> hubContext,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqRealtimeBridgeService> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IModel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.QueueDeclare(_options.RealtimeQueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_options.RealtimeQueueName, _options.ExchangeName, "#");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                if (RealtimeNotificationPayloadFactory.TryCreate(eventArgs.RoutingKey, payload, DateTime.UtcNow, out var message))
                {
                    await hubContext.Clients.Group("broadcast").SendAsync("notification", message, stoppingToken);
                }

                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to bridge realtime message with routing key {RoutingKey}", eventArgs.RoutingKey);
                _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(_options.RealtimeQueueName, autoAck: false, consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        _connection?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}
