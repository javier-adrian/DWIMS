namespace DWIMS.Service.Common;

public sealed class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }

    private Result(T data)
    {
        IsSuccess = true;
        Data = data;
    }
    
    private Result(string error, string errorDescription)
    {
        IsSuccess = false;
        Error = error;
        ErrorDescription = errorDescription;
    }

    public static Result<T> Success(T data) => new(data);
    
    public static Result<T> Failure(string errorCode, string errorMessage) => new(errorCode, errorMessage);
}

public sealed class Result
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
    
    private Result() => IsSuccess = true;
    
    private  Result(string error, string errorDescription)
    {
        IsSuccess = false;
        Error = error;
        ErrorDescription = errorDescription;
    }

    public static Result Success() => new();
    
    public static Result Failure(string error, string errorDescription) => new(error, errorDescription);
}