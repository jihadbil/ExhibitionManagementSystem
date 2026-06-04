using System.Security.Claims;
using ExhibitionManagementSystem.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Base;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
    protected int TenantId
    {
        get
        {
            var claim = User.FindFirstValue("TenantId");
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }

    protected string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    protected ActionResult<T> ToActionResult<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Data);

        return result.ErrorCode switch
        {
            var c when c != null && c.EndsWith("_NOT_FOUND") =>
                NotFound(new { result.ErrorMessage, result.ErrorCode }),
            "EMAIL_ALREADY_EXISTS" or "SUBDOMAIN_ALREADY_EXISTS" or
            "ROLE_ALREADY_ASSIGNED" or "DUPLICATE_RATING" =>
                Conflict(new { result.ErrorMessage, result.ErrorCode }),
            "UNAUTHORIZED" =>
                Unauthorized(new { result.ErrorMessage }),
            _ =>
                BadRequest(new { result.ErrorMessage, result.ErrorCode })
        };
    }

    protected ActionResult ToActionResult(ServiceResult result)
    {
        if (result.IsSuccess)
            return NoContent();

        return result.ErrorCode switch
        {
            var c when c != null && c.EndsWith("_NOT_FOUND") =>
                NotFound(new { result.ErrorMessage, result.ErrorCode }),
            "UNAUTHORIZED" =>
                Unauthorized(new { result.ErrorMessage }),
            _ =>
                BadRequest(new { result.ErrorMessage, result.ErrorCode })
        };
    }
}
