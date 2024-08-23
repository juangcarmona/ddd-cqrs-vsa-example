﻿using Jgcarmona.Qna.Common.Configuration;
using Jgcarmona.Qna.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class RabbitMQListener : IMessagingListener
{
    private readonly RabbitMQSettings _settings;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQListener(IOptions<RabbitMQSettings> settings)
    {
        _settings = settings.Value;

        var factory = new ConnectionFactory()
        {
            HostName = _settings.HostName,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare the exchange
        _channel.ExchangeDeclare(
            exchange: _settings.ExchangeName,
            type: "fanout",  // Use "fanout" to broadcast the message to multiple queues
            durable: true,
            autoDelete: false,
            arguments: null
        );

        // Declare the queue and bind it to the exchange
        _channel.QueueDeclare(queue: _settings.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(queue: _settings.QueueName, exchange: _settings.ExchangeName, routingKey: "");
    }

    public Task StartListeningAsync(Func<string, Task> onMessageReceived, CancellationToken cancellationToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            await onMessageReceived(message);
        };

        _channel.BasicConsume(queue: _settings.QueueName, autoAck: true, consumer: consumer);

        return Task.CompletedTask;
    }
}
