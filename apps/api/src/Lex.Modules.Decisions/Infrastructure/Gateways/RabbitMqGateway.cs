using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Lex.Modules.Decisions.Domain.Gateways;

namespace Lex.Modules.Decisions.Infrastructure.Gateways;

internal sealed class RabbitMqGateway : IMessageBusGateway
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqGateway()
    {
        _factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "lexai_admin",
            Password = "lexai_secret_123",
            // SİHİRLİ DOKUNUŞ: Tıkanmayı (Starvation) engeller, tam asenkron çalışır!
            DispatchConsumersAsync = true 
        };
    }

    private void Connect()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
        }
    }

    public Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default)
    {
        Connect();
        
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        _channel!.QueueDeclare(queue: routingKey, durable: true, exclusive: false, autoDelete: false, arguments: null);
        
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(exchange: "", routingKey: routingKey, basicProperties: properties, body: body);

        return Task.CompletedTask;
    }

    public Task ConsumeAsync<T>(string queueName, Func<T, Task> onMessageReceived, CancellationToken cancellationToken = default)
    {
        Connect();

        _channel!.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        // HIZ SINIRI: 50'den 10'a çektik. Bilgisayar diski rahat nefes alacak.
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        // AsyncEventingBasicConsumer kullanıyoruz!
        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var message = JsonSerializer.Deserialize<T>(json);

            if (message != null)
            {
                try
                {
                    await onMessageReceived(message);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RabbitMQ Error] İşlenirken hata: {ex.Message}");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }
}
