using BackendProject.BLL.Models;
using BackendProject.BLL.Services;
using BackendProject.Validators;
using Microsoft.AspNetCore.Mvc;
using Models.Dto.Common;
using Models.Dto.V1.Requests;
using Models.Dto.V1.Responses;

namespace BackendProject.Controllers.V1;


[Route("api/v1/audit/log-order")]

public class AuditLogController(AuditLogService auditLogService, ValidatorFactory validatorFactory): ControllerBase
{
    [HttpPost("batch-create")]
    public async Task<ActionResult<V1CreateAuditLogOrderRequest>> V1BatchCreate([FromBody] V1CreateAuditLogOrderRequest request,
        CancellationToken token)
    {
        var validationResult = await validatorFactory.GetValidator<V1CreateAuditLogOrderRequest>().ValidateAsync(request, token);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        var res = await auditLogService.BatchInsert(request.Orders.Select(x => new AuditLogOrderUnit
        {
            OrderId = x.OrderId,
            OrderItemId = x.OrderItemId,
            CustomerId = x.CustomerId,
            OrderStatus = x.OrderStatus
        }).ToArray(), token);
        
        return Ok(new V1CreateAuditLogOrderResponse()
        {
            Orders = Map(res)
        });
    }

    private AuditLogOrderUnit[] Map(AuditLogOrderUnit[] logs)
    {
        return logs.Select(x => new AuditLogOrderUnit
        {
            OrderId = x.OrderId,
            OrderItemId = x.OrderItemId,
            CustomerId = x.CustomerId,
            OrderStatus = x.OrderStatus,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        }).ToArray();
    }
}