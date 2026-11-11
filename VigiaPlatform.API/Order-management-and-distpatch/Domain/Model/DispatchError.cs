namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model;

public enum DispatchError
{
    None,
    DispatchNotFound,
    ItemNotFound,
    DuplicateInternalReference,
    InvalidDispatchData,
    InvalidStatusTransition,
    DispatchNotEditable,
    OperationCancelled,
    DatabaseError,
    InternalServerError
}
