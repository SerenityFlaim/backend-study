using BackendProject.BLL.Models;
using BackendProject.DAL;
using BackendProject.DAL.Interfaces;
using BackendProject.DAL.Models;

namespace BackendProject.BLL.Services;

public class OrderService(UnitOfWork unitOfWork, IOrderRepository orderRepository, IOrderItemRepository orderItemRepository)
{
    /// <summary>
    /// Метод создания заказов
    /// </summary>
    public async Task<OrderUnit[]> BatchInsert(OrderUnit[] orderUnits, CancellationToken token)
    {
        var now = DateTimeOffset.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(token);

        try
        {
            // тут ваш бизнес код по инсерту данных в БД
            // нужно положить в БД заказы(orders), а потом их позиции (orderItems)
            // помните, что каждый orderItem содержит ссылку на order (столбец order_id)
            // OrderItem-ов может быть несколько
            var ordersToInsert = orderUnits.Select(ou => new V1OrderDal //конвертация BLL объектов в DAL объекты
            {
                CustomerId = ou.CustomerId,
                DeliveryAddress = ou.DeliveryAddress,
                TotalPriceCents = ou.TotalPriceCents,
                TotalPriceCurrency = ou.TotalPriceCurrency,
                CreatedAt = now,
                UpdatedAt = now
            }).ToArray();
            
            var insertedOrders = await orderRepository.BulkInsert(ordersToInsert, token);
            
            var orderItemsToInsert = orderUnits
                .SelectMany((ou, orderIndex) =>
                    (ou.OrderItems ?? Array.Empty<OrderItemUnit>())
                        .Select(oi => new V1OrderItemDal
                         {
                            OrderId = insertedOrders[orderIndex].Id,
                            ProductId = oi.ProductId,
                            Quantity = oi.Quantity,
                            ProductTitle = oi.ProductTitle,
                            ProductUrl = oi.ProductUrl,
                            PriceCents = oi.PriceCents,
                            PriceCurrency = oi.PriceCurrency,
                            CreatedAt = now,
                            UpdatedAt = now
                         })
                ).ToArray();
            
            V1OrderItemDal[] insertedOrderItems = Array.Empty<V1OrderItemDal>();
            if (insertedOrders.Length > 0)
            {
                insertedOrderItems = await orderItemRepository.BulkInsert(orderItemsToInsert, token);
            }
            
            await transaction.CommitAsync(token);
            
            var orderItemLookup = insertedOrderItems.ToLookup(x => x.OrderId);

            return Map(insertedOrders, orderItemLookup);
        }
        catch (Exception e) 
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }
    
    /// <summary>
    /// Метод получения заказов
    /// </summary>
    public async Task<OrderUnit[]> GetOrders(QueryOrderItemsModel model, CancellationToken token)
    {
        var orders = await orderRepository.Query(new QueryOrdersDalModel
        {
            Ids = model.Ids,
            CustomerIds = model.CustomerIds,
            Limit = model.PageSize,
            Offset = (model.Page - 1) * model.PageSize
        }, token);

        if (orders.Length is 0)
        {
            return [];
        }
        
        ILookup<long, V1OrderItemDal> orderItemLookup = null;
        if (model.IncludeOrderItems)
        {
            var orderItems = await orderItemRepository.Query(new QueryOrderItemsDalModel
            {
                OrderIds = orders.Select(x => x.Id).ToArray(),
            }, token);

            orderItemLookup = orderItems.ToLookup(x => x.OrderId);
        }

        return Map(orders, orderItemLookup);
    }
    
    private OrderUnit[] Map(V1OrderDal[] orders, ILookup<long, V1OrderItemDal> orderItemLookup = null)
    {
        return orders.Select(x => new OrderUnit
        {
            Id = x.Id,
            CustomerId = x.CustomerId,
            DeliveryAddress = x.DeliveryAddress,
            TotalPriceCents = x.TotalPriceCents,
            TotalPriceCurrency = x.TotalPriceCurrency,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            OrderItems = orderItemLookup?[x.Id].Select(o => new OrderItemUnit
            {
                Id = o.Id,
                OrderId = o.OrderId,
                ProductId = o.ProductId,
                Quantity = o.Quantity,
                ProductTitle = o.ProductTitle,
                ProductUrl = o.ProductUrl,
                PriceCents = o.PriceCents,
                PriceCurrency = o.PriceCurrency,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            }).ToArray() ?? []
        }).ToArray();
    }
}