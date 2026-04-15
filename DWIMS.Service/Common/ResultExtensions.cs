using DWIMS.Service.Common;
using Microsoft.AspNetCore.Http;

namespace DWIMS.Service.Services;

public static class ResultExtensions
{
    public static IResult ToOkResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Data)
            : Results.UnprocessableEntity(new
            {
                result.Error,
                result.ErrorDescription
            });
    
    public static IResult ToCreatedResult<T>(
        this Result<T> result,
        string location) =>
        result.IsSuccess
            ? Results.Created(location, result.Data)
            : Results.UnprocessableEntity(new
            {
                result.Error,
                result.ErrorDescription
            });
}