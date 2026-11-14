using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.Order_management_and_distpatch.Application.CommandServices;
using VigiaPlatform.API.Order_management_and_distpatch.Application.QueryServices;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Queries;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "ADMIN,WAREHOUSE_MANAGER")]
[ApiController]
[Route("api/v1/orders")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Orders")]
public class OrdersController(
    IOrderCommandService orderCommandService,
    IOrderQueryService orderQueryService) : ControllerBase
{
    /// <summary>
    ///     Gets all orders, optionally filtered by status or code.
    /// </summary>
    [HttpGet]
    [EndpointSummary("Get all orders")]
    [EndpointDescription("Get all orders. Optional filters: status (PENDING, UNDER_REVIEW, APPROVED, REJECTED) or code (SOL-###).")]
    [ProducesResponseType(typeof(IEnumerable<OrderResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] string? status,
        [FromQuery] string? code,
        CancellationToken cancellationToken)
    {
        IEnumerable<Order> orders;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatusType>(status.Replace("_", string.Empty), true, out var parsedStatus))
                return BadRequest($"Invalid status '{status}'");

            orders = await orderQueryService.Handle(new GetOrdersByStatusQuery(parsedStatus), cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(code))
        {
            var order = await orderQueryService.Handle(new GetOrderByCodeQuery(code), cancellationToken);
            orders = order is null ? [] : [order];
        }
        else
        {
            orders = await orderQueryService.Handle(new GetAllOrdersQuery(), cancellationToken);
        }

        return Ok(orders.Select(OrderResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets an order by id.
    /// </summary>
    [HttpGet("{orderId:int}")]
    [EndpointSummary("Get an order by id")]
    [EndpointDescription("Get an order with its items, attached files and timeline by its unique identifier.")]
    [ProducesResponseType(typeof(OrderResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(int orderId, CancellationToken cancellationToken)
    {
        var order = await orderQueryService.Handle(new GetOrderByIdQuery(orderId), cancellationToken);

        return OrderActionResultAssembler.ToActionResultFromGetOrderResult(
            this,
            order,
            found => Ok(OrderResourceFromEntityAssembler.ToResourceFromEntity(found)));
    }

    /// <summary>
    ///     Creates an order.
    /// </summary>
    [HttpPost]
    [EndpointSummary("Create an order")]
    [EndpointDescription("Create a new material order with its items. It starts with the PENDING status.")]
    [ProducesResponseType(typeof(OrderResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = CreateOrderCommandFromResourceAssembler.ToCommandFromResource(resource);
            var result = await orderCommandService.Handle(command, cancellationToken);

            return OrderActionResultAssembler.ToActionResultFromOrderResult(
                this,
                result,
                created => CreatedAtAction(nameof(GetOrderById), new { orderId = created.Id },
                    OrderResourceFromEntityAssembler.ToResourceFromEntity(created)));
        }
        catch (ArgumentException exception)
        {
            return OrderActionResultAssembler.ToBadRequest(this, exception);
        }
    }

    /// <summary>
    ///     Attaches a file to an order.
    /// </summary>
    [HttpPost("{orderId:int}/attached-files")]
    [EndpointSummary("Attach a file to an order")]
    [EndpointDescription("Attach a file to an order that has not been approved or rejected yet.")]
    [ProducesResponseType(typeof(OrderResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddOrderAttachedFile(int orderId,
        [FromBody] AddOrderAttachedFileResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = OrderCommandFromResourceAssembler.ToCommandFromResource(orderId, resource with { PerformedBy = User.Identity!.Name! });
            var result = await orderCommandService.Handle(command, cancellationToken);

            return OrderActionResultAssembler.ToActionResultFromOrderResult(
                this,
                result,
                updated => Ok(OrderResourceFromEntityAssembler.ToResourceFromEntity(updated)));
        }
        catch (ArgumentException exception)
        {
            return OrderActionResultAssembler.ToBadRequest(this, exception);
        }
    }

    /// <summary>
    ///     Changes the status of an order.
    /// </summary>
    [HttpPatch("{orderId:int}/status")]
    [EndpointSummary("Change the status of an order")]
    [EndpointDescription("Move the order forward: UNDER_REVIEW, then APPROVED or REJECTED. A reason is required to reject.")]
    [ProducesResponseType(typeof(OrderResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeOrderStatus(int orderId, [FromBody] ChangeOrderStatusResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = OrderCommandFromResourceAssembler.ToCommandFromResource(orderId, resource with { PerformedBy = User.Identity!.Name! });
            var result = await orderCommandService.Handle(command, cancellationToken);

            return OrderActionResultAssembler.ToActionResultFromOrderResult(
                this,
                result,
                updated => Ok(OrderResourceFromEntityAssembler.ToResourceFromEntity(updated)));
        }
        catch (ArgumentException exception)
        {
            return OrderActionResultAssembler.ToBadRequest(this, exception);
        }
    }
}
