using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model;
using VigiaPlatform.API.Shared.Application.Patterns;

namespace VigiaPlatform.API.Order_management_and_distpatch.Application.CommandServices;

public interface IDispatchCommandService
{
    Task<Result<Dispatch, DispatchError>> Handle(CreateDispatchCommand command, CancellationToken cancellationToken = default);
    Task<Result<Dispatch, DispatchError>> Handle(AddDispatchItemCommand command, CancellationToken cancellationToken = default);
    Task<Result<Dispatch, DispatchError>> Handle(RemoveDispatchItemCommand command, CancellationToken cancellationToken = default);
    Task<Result<Dispatch, DispatchError>> Handle(AssignVehicleCommand command, CancellationToken cancellationToken = default);
    Task<Result<Dispatch, DispatchError>> Handle(ChangeDispatchStatusCommand command, CancellationToken cancellationToken = default);
    Task<Result<Dispatch, DispatchError>> Handle(CancelDispatchCommand command, CancellationToken cancellationToken = default);
}
