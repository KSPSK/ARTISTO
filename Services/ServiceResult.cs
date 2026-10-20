namespace ARTISTO.Services;

public enum ServiceStatus
{
    Success,
    NotFound,
    Forbidden,
    Conflict,
    BadRequest,
    Unauthorized
}

public class ServiceResult
{
    public ServiceStatus Status { get; }
    public string? ErrorMessage { get; }
    public bool IsSuccess => Status == ServiceStatus.Success;

    public ServiceResult(ServiceStatus status, string? errorMessage = null)
    {
        Status = status;
        ErrorMessage = errorMessage;
    }

    public static ServiceResult Success() => new(ServiceStatus.Success);
    public static ServiceResult NotFound(string message) => new(ServiceStatus.NotFound, message);
    public static ServiceResult Forbidden(string? message = null) => new(ServiceStatus.Forbidden, message);
    public static ServiceResult Conflict(string message) => new(ServiceStatus.Conflict, message);
    public static ServiceResult BadRequest(string message) => new(ServiceStatus.BadRequest, message);
    public static ServiceResult Unauthorized(string message) => new(ServiceStatus.Unauthorized, message);
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; }

    public ServiceResult(T? value, ServiceStatus status, string? errorMessage = null)
        : base(status, errorMessage)
    {
        Value = value;
    }

    public static ServiceResult<T> Success(T value) => new(value, ServiceStatus.Success);
    public new static ServiceResult<T> NotFound(string message) => new(default, ServiceStatus.NotFound, message);
    public new static ServiceResult<T> Forbidden(string? message = null) => new(default, ServiceStatus.Forbidden, message);
    public new static ServiceResult<T> Conflict(string message) => new(default, ServiceStatus.Conflict, message);
    public new static ServiceResult<T> BadRequest(string message) => new(default, ServiceStatus.BadRequest, message);
    public new static ServiceResult<T> Unauthorized(string message) => new(default, ServiceStatus.Unauthorized, message);
}
