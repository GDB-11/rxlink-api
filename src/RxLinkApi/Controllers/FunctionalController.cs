using System.Security.Claims;
using BindSharp;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Extensions;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

/// <summary>
/// Base controller with functional programming patterns.
/// Handles Result → HTTP conversion and logging as side effects.
/// </summary>
[ApiController]
public abstract class FunctionalController : ControllerBase
{
    private readonly IResultLogger _logger;

    protected FunctionalController(IResultLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes an operation and converts the Result to an HTTP response.
    /// Logging happens as a side effect at the edge.
    /// </summary>
    protected IActionResult Execute<T, TError>(
        Func<Result<T, TError>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName,
        Func<T, IActionResult>? successMapper = null)
    {
        return operation()
            .LogResult(_logger, operationName)
            .ToHttpResult(errorMapper, successMapper);
    }

    /// <summary>
    /// Executes an async operation and converts the Result to an HTTP response.
    /// </summary>
    protected async Task<IActionResult> ExecuteAsync<T, TError>(
        Func<Task<Result<T, TError>>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName,
        Func<T, IActionResult>? successMapper = null)
    {
        Result<T, TError> result = await operation();

        return result
            .LogResult(_logger, operationName)
            .ToHttpResult(errorMapper, successMapper);
    }

    /// <summary>
    /// Executes an async operation with authenticated user context.
    /// Automatically extracts and validates the user ID (<c>sub</c> claim) from the JWT.
    /// </summary>
    protected async Task<IActionResult> ExecuteAuthenticatedAsync<T, TError>(
        Func<Guid, Task<Result<T, TError>>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName,
        Func<T, IActionResult>? successMapper = null)
    {
        string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
        {
            await _logger.LogErrorAsync(operationName, "Invalid or missing user ID in JWT token");
            return Unauthorized();
        }

        Result<T, TError> result = await operation(userId);

        return result
            .LogResult(_logger, operationName)
            .ToHttpResult(errorMapper, successMapper);
    }

    /// <summary>
    /// Executes an async operation with the authenticated user's role.
    /// Extracts and validates the <see cref="ClaimTypes.Role"/> claim from the JWT,
    /// then passes the role name to <paramref name="operation"/>.
    /// Returns <c>401 Unauthorized</c> when the claim is absent or empty.
    /// </summary>
    protected async Task<IActionResult> ExecuteWithRoleAsync<T, TError>(
        Func<string, Task<Result<T, TError>>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName,
        Func<T, IActionResult>? successMapper = null)
    {
        string? roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(roleClaim))
        {
            await _logger.LogErrorAsync(operationName, "Invalid or missing role claim in JWT token");
            return Unauthorized();
        }

        Result<T, TError> result = await operation(roleClaim);

        return result
            .LogResult(_logger, operationName)
            .ToHttpResult(errorMapper, successMapper);
    }

    /// <summary>
    /// Executes an operation that returns NoContent on success.
    /// </summary>
    protected IActionResult ExecuteNoContent<T, TError>(
        Func<Result<T, TError>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName)
    {
        return operation()
            .LogResult(_logger, operationName)
            .ToNoContentResult(errorMapper);
    }

    /// <summary>
    /// Executes an operation that returns Created (201) on success.
    /// </summary>
    protected IActionResult ExecuteCreated<T, TError>(
        Func<Result<T, TError>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName,
        Func<T, string> locationFactory)
    {
        return operation()
            .LogResult(_logger, operationName)
            .ToCreatedResult(errorMapper, locationFactory);
    }

    /// <summary>
    /// Executes an operation with a custom success status code.
    /// </summary>
    protected IActionResult ExecuteWithStatus<T, TError>(
        Func<Result<T, TError>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName,
        int successStatusCode)
    {
        return operation()
            .LogResult(_logger, operationName)
            .ToHttpResult(errorMapper, successStatusCode);
    }
}