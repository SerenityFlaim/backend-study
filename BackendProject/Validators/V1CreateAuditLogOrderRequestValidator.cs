using FluentValidation;
using Models.Dto.V1.Requests;

namespace BackendProject.Validators;

public class V1CreateAuditLogOrderRequestValidator : AbstractValidator<V1AuditLogOrderRequest>
{
    public V1CreateAuditLogOrderRequestValidator()
    {
        RuleFor(x => x.Orders).NotEmpty();
        RuleForEach(x => x.Orders).NotNull();
        RuleForEach(x => x.Orders).ChildRules(order =>
        {
            order.RuleFor(o => o.OrderItemId).GreaterThan(0);
            order.RuleFor(o => o.OrderId).GreaterThan(0);
            order.RuleFor(o => o.CustomerId).GreaterThan(0);
            order.RuleFor(o => o.OrderStatus).NotEmpty();
        });
    }
}