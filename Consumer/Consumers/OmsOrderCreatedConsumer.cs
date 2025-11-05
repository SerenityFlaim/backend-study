using System.Text;
using Consumer.Config;
using Consumer.Clients;
using Microsoft.Extensions.Options;
using Models.Dto.V1.Requests;
using Models.Enums;
using Newtonsoft.Json.Linq;
using Project.Common;
using Project.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Consumer.Consumers;

public class OmsOrderCreatedConsumer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<RabbitMqSettings> _rabbitMqSettings;
    private readonly ConnectionFactory _factory;
    private IConnection _connection;
    private IChannel _channel;
    private AsyncEventingBasicConsumer _consumer;
    
    public OmsOrderCreatedConsumer(IOptions<RabbitMqSettings> rabbitMqSettings, IServiceProvider serviceProvider)
    {
        _rabbitMqSettings = rabbitMqSettings;
        _serviceProvider = serviceProvider;
        _factory = new ConnectionFactory { HostName = rabbitMqSettings.Value.HostName, Port = rabbitMqSettings.Value.Port };
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _connection = await _factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(
            queue: _rabbitMqSettings.Value.OrderCreatedQueue, 
            durable: false, 
            exclusive: false,
            autoDelete: false,
            arguments: null, 
            cancellationToken: cancellationToken);

        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += async (sender, args) =>
        {
            var body = args.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var order = message.FromJson<OrderCreatedMessage>();
            
            Console.WriteLine($"Received order: {order}");
            Console.WriteLine($"Order.Id = {order.Id}");
            Console.WriteLine($"Order.CustomerId = {order.CustomerId}");
            Console.WriteLine($"OrderItems.Count = {order.OrderItems?.Length ?? 0}");
            foreach (var item in order.OrderItems)
            {
                Console.WriteLine(
                    $"Sending to OMS -> OrderId: {order.Id}, OrderItemId: {item.Id}, CustomerId: {order.CustomerId}, Status: Created");
            }

            Console.WriteLine("Received: " + message);
            
            var j = JToken.Parse(message);
            Console.WriteLine("order_items present? " + (j["order_items"] != null));
            Console.WriteLine("order_items token type: " + j["order_items"]?.Type);
            Console.WriteLine("order_items count: " + (j["order_items"]?.Count() ?? 0));
            
            using var scope = _serviceProvider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<OmsClient>();
            await client.LogOrder(new V1AuditLogOrderRequest
            {
                Orders = order.OrderItems.Select(x => 
                    new V1AuditLogOrderRequest.LogOrder
                    {
                        OrderId = order.Id,
                        OrderItemId = x.Id,
                        CustomerId = order.CustomerId,
                        OrderStatus = nameof(OrderStatus.Created)
                    }).ToArray()
            }, CancellationToken.None);
        };
        
        await _channel.BasicConsumeAsync(
            queue: _rabbitMqSettings.Value.OrderCreatedQueue, 
            autoAck: true, 
            consumer: _consumer,
            cancellationToken: cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        _connection?.Dispose();
        _channel?.Dispose();
    }
}