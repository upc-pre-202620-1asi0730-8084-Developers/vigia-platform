using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Shared.Application.Patterns;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

/// <summary>
///     Transforms the results of the order services into HTTP action results.
/// </summary>
public static class OrderActionResultAssembler
{
    private static int ToStatusCodeFromOrderError(OrderError error) => error switch
    {
        OrderError.OrderNotFound => StatusCodes.Status404NotFound,
        OrderError.DuplicateOrderCode => StatusCodes.Status409Conflict,
        OrderError.InvalidStatusTransition => StatusCodes.Status409Conflict,
        OrderError.OrderNotEditable => StatusCodes.Status409Conflict,
        OrderError.OperationCancelled => StatusCodes.Status409Conflict,
        OrderError.DatabaseError => StatusCodes.Status500InternalServerError,
        OrderError.InternalServerError => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status400BadRequest
    };

    public static IActionResult ToActionResultFromOrderResult(
        ControllerBase controller,
        Result<Order, OrderError> result,
        Func<Order, IActionResult> successAction)
    {
        return result.Fold(
            successAction,
            error => controller.Problem(
                statusCode: ToStatusCodeFromOrderError(error),
                title: error.ToString()));
    }

    public static IActionResult ToActionResultFromGetOrderResult(
        ControllerBase controller,
        Order? order,
        Func<Order, IActionResult> successAction)
    {
        return order is null
            ? controller.Problem(
                statusCode: ToStatusCodeFromOrderError(OrderError.OrderNotFound),
                title: OrderError.OrderNotFound.ToString())
            : successAction(order);
    }

    public static IActionResult ToBadRequest(ControllerBase controller, ArgumentException exception) =>
        controller.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: OrderError.InvalidOrderData.ToString(),
            detail: exception.Message);
}
