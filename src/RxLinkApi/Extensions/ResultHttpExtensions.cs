using BindSharp;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Mappings;

namespace RxLinkApi.Extensions;

/// <summary>
/// Functional extensions for converting Results to HTTP responses
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// Converts a Result to an IActionResult using a mapper for errors
    /// </summary>
    public static IActionResult ToHttpResult<T, TError>(
        this Result<T, TError> result,
        IErrorHttpMapper<TError> errorMapper,
        Func<T, IActionResult>? successMapper = null) =>
        result.Match(
            value => successMapper?.Invoke(value) ?? new OkObjectResult(value),
            error => errorMapper.MapToHttp(error)
        );

    /// <summary>
    /// Converts a Result to an IActionResult asynchronously
    /// </summary>
    public static async Task<IActionResult> ToHttpResultAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        IErrorHttpMapper<TError> errorMapper,
        Func<T, IActionResult>? successMapper = null)
    {
        Result<T, TError> result = await resultTask;
        return result.ToHttpResult(errorMapper, successMapper);
    }

    /// <summary>
    /// Converts a Result to an IActionResult with a custom success status code
    /// </summary>
    public static IActionResult ToHttpResult<T, TError>(
        this Result<T, TError> result,
        IErrorHttpMapper<TError> errorMapper,
        int successStatusCode) =>
        result.Match(
            value => new ObjectResult(value) { StatusCode = successStatusCode },
            error => errorMapper.MapToHttp(error)
        );

    /// <summary>
    /// Converts a Result to NoContent (204) on success
    /// </summary>
    public static IActionResult ToNoContentResult<T, TError>(
        this Result<T, TError> result,
        IErrorHttpMapper<TError> errorMapper) =>
        result.Match(
            _ => new NoContentResult(),
            error => errorMapper.MapToHttp(error)
        );

    /// <summary>
    /// Converts a Result to Created (201) on success with a location
    /// </summary>
    public static IActionResult ToCreatedResult<T, TError>(
        this Result<T, TError> result,
        IErrorHttpMapper<TError> errorMapper,
        Func<T, string> locationFactory) =>
        result.Match(
            value => new CreatedResult(locationFactory(value), value),
            error => errorMapper.MapToHttp(error)
        );
}