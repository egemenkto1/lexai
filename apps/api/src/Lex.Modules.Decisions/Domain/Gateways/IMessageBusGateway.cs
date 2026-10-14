using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lex.Modules.Decisions.Domain.Gateways;

public interface IMessageBusGateway
{
    // C# içinde bir olayı kuyruğa fırlatmak istersek
    Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default);
    
    // Kuyruğu dinleyip gelen mesajları işlemek için
    Task ConsumeAsync<T>(string queueName, Func<T, Task> onMessageReceived, CancellationToken cancellationToken = default);
}
