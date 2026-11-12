using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Shared.Application.Patterns;

namespace VigiaPlatform.API.Order_management_and_distpatch.Application.CommandServices;

public interface IOrderCommandService
{
    Task<Result<Order, OrderError>> Handle(CreateOrderCommand command, CancellationToken cancellationToken = default);
    Task<Result<Order, OrderError>> Handle(AddOrderAttachedFileCommand command, CancellationToken cancellationToken = default);
    Task<Result<Order, OrderError>> Handle(ChangeOrderStatusCommand command, CancellationToken cancellationToken = default);
}
