
using Models.Dto.Common;
using BackendProject.DAL;
using BackendProject.DAL.Interfaces;
using BackendProject.DAL.Models;

namespace BackendProject.BLL.Services;


public class AuditLogService(UnitOfWork unitOfWork, IAuditLogOrderRepository auditLogRepository)
{
    public async Task<AuditLogOrderUnit[]> BatchInsert(AuditLogOrderUnit[] logUnits, CancellationToken token)
    {
        var now = DateTimeOffset.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(token);

        try
        {
            var logsToInsert = logUnits.Select(lu => new V1AuditLogOrderDal
            {
                OrderId = lu.OrderId,
                OrderItemId = lu.OrderItemId,
                CustomerId = lu.CustomerId,
                OrderStatus = lu.OrderStatus,
                CreatedAt = now,
                UpdatedAt = now
            }).ToArray();

            var insertedLogs = await auditLogRepository.BulkInsert(logsToInsert, token);
            await transaction.CommitAsync(token);
            var result = insertedLogs.Select(x => new AuditLogOrderUnit
            {
                OrderId = x.OrderId,
                OrderItemId = x.OrderItemId,
                CustomerId = x.CustomerId,
                OrderStatus = x.OrderStatus,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToArray();

            return result;
        }
        catch 
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }
}
