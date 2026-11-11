namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model;

public enum OrderError
{
    None,
    OrderNotFound,
    DuplicateOrderCode,
    InvalidOrderData,
    InvalidStatusTransition,
    OrderNotEditable,
    OperationCancelled,
    DatabaseError,
    InternalServerError
}
