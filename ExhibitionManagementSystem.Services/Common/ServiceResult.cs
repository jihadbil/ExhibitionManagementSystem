using System;

namespace ExhibitionManagementSystem.Services.Common;

public class ServiceResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public string? ErrorCode { get; }

    protected ServiceResult(bool isSuccess, string? errorMessage = null, string? errorCode = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    public static ServiceResult Success() => new ServiceResult(true);
    public static ServiceResult Success(string message) => new ServiceResult(true, message);
    public static ServiceResult Failure(string errorMessage, string? errorCode = null) => new ServiceResult(false, errorMessage, errorCode);
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; }

    private ServiceResult(bool isSuccess, T? data = default, string? errorMessage = null, string? errorCode = null)
        : base(isSuccess, errorMessage, errorCode)
    {
        Data = data;
    }

    public static ServiceResult<T> Success(T data) => new ServiceResult<T>(true, data);
    public static new ServiceResult<T> Failure(string errorMessage, string? errorCode = null) => new ServiceResult<T>(false, default, errorMessage, errorCode);
}
