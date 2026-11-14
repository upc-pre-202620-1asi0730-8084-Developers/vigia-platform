using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Order_management_and_distpatch.Application.CommandServices;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;
using VigiaPlatform.API.Shared.Application.Patterns;
using VigiaPlatform.API.Shared.Domain.Repositories;

namespace VigiaPlatform.API.Order_management_and_distpatch.Application.Internal.CommandServices;

public class OrderCommandService(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : IOrderCommandService
{
    public async Task<Result<Order, OrderError>> Handle(CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            Order order;
            try
            {
                order = new Order(command, DateTime.UtcNow);
            }
            catch (ArgumentException)
            {
                return Failure(OrderError.InvalidOrderData);
            }

            if (await orderRepository.ExistsByCodeAsync(order.Code, cancellationToken))
                return Failure(OrderError.DuplicateOrderCode);

            await orderRepository.AddAsync(order, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Success(order);
        });
    }

    public async Task<Result<Order, OrderError>> Handle(AddOrderAttachedFileCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ModifyAsync(command.OrderId, requireEditable: true, cancellationToken,
            order => order.AddAttachedFile(command.Name, command.Type, command.SizeInBytes, command.PerformedBy,
                DateTime.UtcNow));
    }

    public async Task<Result<Order, OrderError>> Handle(ChangeOrderStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ModifyAsync(command.OrderId, requireEditable: false, cancellationToken,
            order => order.ChangeStatus(command.NewStatus, command.PerformedBy, command.Description,
                DateTime.UtcNow),
            invalidOperationError: OrderError.InvalidStatusTransition);
    }

    /// <summary>
    ///     Loads the order, applies the change, persists it and maps domain exceptions to errors.
    /// </summary>
    private async Task<Result<Order, OrderError>> ModifyAsync(
        int orderId,
        bool requireEditable,
        CancellationToken cancellationToken,
        Action<Order> change,
        OrderError invalidOperationError = OrderError.InvalidOrderData)
    {
        return await ExecuteAsync(async () =>
        {
            var order = await orderRepository.FindByIdAsync(orderId, cancellationToken);
            if (order is null) return Failure(OrderError.OrderNotFound);

            if (requireEditable && order.Status is not (OrderStatusType.Pending or OrderStatusType.UnderReview))
                return Failure(OrderError.OrderNotEditable);

            try
            {
                change(order);
            }
            catch (ArgumentException)
            {
                return Failure(OrderError.InvalidOrderData);
            }
            catch (InvalidOperationException)
            {
                return Failure(invalidOperationError);
            }

            orderRepository.Update(order);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Success(order);
        });
    }

    private static async Task<Result<Order, OrderError>> ExecuteAsync(
        Func<Task<Result<Order, OrderError>>> action)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            return Failure(OrderError.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return Failure(OrderError.DatabaseError);
        }
        catch (Exception)
        {
            return Failure(OrderError.InternalServerError);
        }
    }

    private static Result<Order, OrderError> Success(Order order) =>
        new Result<Order, OrderError>.Success(order);

    private static Result<Order, OrderError> Failure(OrderError error) =>
        new Result<Order, OrderError>.Failure(error);
}
