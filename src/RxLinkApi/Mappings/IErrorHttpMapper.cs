using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

/// <summary>
/// Maps domain errors to HTTP responses
/// </summary>
public interface IErrorHttpMapper<in TError>
{
    IActionResult MapToHttp(TError error);
}