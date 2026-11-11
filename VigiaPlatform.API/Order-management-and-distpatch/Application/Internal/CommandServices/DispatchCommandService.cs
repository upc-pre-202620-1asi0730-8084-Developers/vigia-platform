using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Order_management_and_distpatch.Application.CommandServices;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;
using VigiaPlatform.API.Shared.Application.Patterns;
using VigiaPlatform.API.Shared.Domain.Repositories;

namespace VigiaPlatform.API.Order_management_and_distpatch.Application.Internal.CommandServices;

public class DispatchCommandService(
    IDispatchRepository dispatchRepository,
    IUnitOfWork unitOfWork) : IDispatchCommandService
{
    public async Task<Result<Dispatch, DispatchError>> Handle(CreateDispatchCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            Dispatch dispatch;
            try
            {
                dispatch = new Dispatch(command, DateTime.UtcNow);
            }
            catch (ArgumentException)
            {
                return Failure(DispatchError.InvalidDispatchData);
            }

            if (await dispatchRepository.ExistsByInternalReferenceAsync(dispatch.InternalReference,
                    cancellationToken))
                return Failure(DispatchError.DuplicateInternalReference);

            await dispatchRepository.AddAsync(dispatch, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Success(dispatch);
        });
    }

    public async Task<Result<Dispatch, DispatchError>> Handle(AddDispatchItemCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ModifyAsync(command.DispatchId, requireEditable: true, cancellationToken,
            dispatch => dispatch.AddItem(command, DateTime.UtcNow));
    }

    public async Task<Result<Dispatch, DispatchError>> Handle(RemoveDispatchItemCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ModifyAsync(command.DispatchId, requireEditable: true, cancellationToken, dispatch =>
        {
            if (dispatch.Items.All(i => i.Id != command.ItemId))
                throw new KeyNotFoundException($"Item {command.ItemId} not found in dispatch.");

            dispatch.RemoveItem(command.ItemId, command.PerformedBy, DateTime.UtcNow);
        });
    }

    public async Task<Result<Dispatch, DispatchError>> Handle(AssignVehicleCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ModifyAsync(command.DispatchId, requireEditable: true, cancellationToken,
            dispatch => dispatch.AssignVehicle(command.VehicleId, command.VehiclePlate, command.PerformedBy,
                DateTime.UtcNow));
    }

    public async Task<Result<Dispatch, DispatchError>> Handle(ChangeDispatchStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ModifyAsync(command.DispatchId, requireEditable: false, cancellationToken,
            dispatch => dispatch.ChangeStatus(command.NewStatus, command.PerformedBy, command.Description,
                DateTime.UtcNow),
            invalidOperationError: DispatchError.InvalidStatusTransition);
    }

    public async Task<Result<Dispatch, DispatchError>> Handle(CancelDispatchCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ModifyAsync(command.DispatchId, requireEditable: false, cancellationToken,
            dispatch => dispatch.Cancel(command.Reason, command.PerformedBy, DateTime.UtcNow),
            invalidOperationError: DispatchError.InvalidStatusTransition);
    }

    /// <summary>
    ///     Loads the dispatch, applies the change, persists it and maps domain exceptions to errors.
    /// </summary>
    private async Task<Result<Dispatch, DispatchError>> ModifyAsync(
        int dispatchId,
        bool requireEditable,
        CancellationToken cancellationToken,
        Action<Dispatch> change,
        DispatchError invalidOperationError = DispatchError.InvalidDispatchData)
    {
        return await ExecuteAsync(async () =>
        {
            var dispatch = await dispatchRepository.FindByIdAsync(dispatchId, cancellationToken);
            if (dispatch is null) return Failure(DispatchError.DispatchNotFound);

            if (requireEditable && dispatch.Status is not (DispatchStatus.Scheduled or DispatchStatus.InPreparation))
                return Failure(DispatchError.DispatchNotEditable);

            try
            {
                change(dispatch);
            }
            catch (KeyNotFoundException)
            {
                return Failure(DispatchError.ItemNotFound);
            }
            catch (ArgumentException)
            {
                return Failure(DispatchError.InvalidDispatchData);
            }
            catch (InvalidOperationException)
            {
                return Failure(invalidOperationError);
            }

            dispatchRepository.Update(dispatch);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Success(dispatch);
        });
    }

    private static async Task<Result<Dispatch, DispatchError>> ExecuteAsync(
        Func<Task<Result<Dispatch, DispatchError>>> action)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            return Failure(DispatchError.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return Failure(DispatchError.DatabaseError);
        }
        catch (Exception)
        {
            return Failure(DispatchError.InternalServerError);
        }
    }

    private static Result<Dispatch, DispatchError> Success(Dispatch dispatch) =>
        new Result<Dispatch, DispatchError>.Success(dispatch);

    private static Result<Dispatch, DispatchError> Failure(DispatchError error) =>
        new Result<Dispatch, DispatchError>.Failure(error);
}
