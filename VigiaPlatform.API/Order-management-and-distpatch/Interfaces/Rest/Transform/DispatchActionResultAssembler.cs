using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Shared.Application.Patterns;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

/// <summary>
///     Transforms the results of the dispatch services into HTTP action results.
/// </summary>
public static class DispatchActionResultAssembler
{
    private static int ToStatusCodeFromDispatchError(DispatchError error) => error switch
    {
        DispatchError.DispatchNotFound => StatusCodes.Status404NotFound,
        DispatchError.ItemNotFound => StatusCodes.Status404NotFound,
        DispatchError.DuplicateInternalReference => StatusCodes.Status409Conflict,
        DispatchError.InvalidStatusTransition => StatusCodes.Status409Conflict,
        DispatchError.DispatchNotEditable => StatusCodes.Status409Conflict,
        DispatchError.OperationCancelled => StatusCodes.Status409Conflict,
        DispatchError.DatabaseError => StatusCodes.Status500InternalServerError,
        DispatchError.InternalServerError => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status400BadRequest
    };

    public static IActionResult ToActionResultFromDispatchResult(
        ControllerBase controller,
        Result<Dispatch, DispatchError> result,
        Func<Dispatch, IActionResult> successAction)
    {
        return result.Fold(
            successAction,
            error => controller.Problem(
                statusCode: ToStatusCodeFromDispatchError(error),
                title: error.ToString()));
    }

    public static IActionResult ToActionResultFromGetDispatchByIdResult(
        ControllerBase controller,
        Dispatch? dispatch,
        Func<Dispatch, IActionResult> successAction)
    {
        return dispatch is null
            ? controller.Problem(
                statusCode: ToStatusCodeFromDispatchError(DispatchError.DispatchNotFound),
                title: DispatchError.DispatchNotFound.ToString())
            : successAction(dispatch);
    }

    public static IActionResult ToBadRequest(ControllerBase controller, ArgumentException exception) =>
        controller.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: DispatchError.InvalidDispatchData.ToString(),
            detail: exception.Message);
}
