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
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("nameid")
        ?? User.FindFirstValue("sub")
        ?? string.Empty;

    protected ActionResult<T> ToActionResult<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Data);

        return result.ErrorCode switch
        {
            var c when c != null && c.EndsWith("_NOT_FOUND") =>
                NotFound(new { result.ErrorMessage, result.ErrorCode }),
            "VENUE_CONFLICT" or "VENUE_DELETE_CONFLICT" or "EMAIL_ALREADY_EXISTS" or "SUBDOMAIN_ALREADY_EXISTS" or
            "REGISTRATION_CONFLICT" or "ROLE_ALREADY_ASSIGNED" or "DUPLICATE_RATING" or "SITE_PLAN_CONFLICT" or
            "SCHEDULE_CAPACITY_CONFLICT" or "SCHEDULE_HAS_REGISTRATIONS" or "SCHEDULE_CONFLICT" or "TICKET_ISSUE_CONFLICT" or "EXHIBITION_DATES_LOCKED" or
            "PACKAGE_CAPACITY_CONFLICT" or "PACKAGE_HAS_CONTRACTS" or "SPACE_HAS_ASSIGNMENTS" or
            "SPONSOR_HAS_CONTRACTS" or "SPONSOR_DELETE_CONFLICT" or "SPONSORSHIP_CONFLICT" =>
                Conflict(new { result.ErrorMessage, result.ErrorCode }),
            "SUBSCRIPTION_PLAN_CHANGE_NOT_ALLOWED" =>
                StatusCode(StatusCodes.Status403Forbidden, new { result.ErrorMessage, result.ErrorCode }),
            "UNAUTHORIZED" or "SELF_MANAGEMENT_FORBIDDEN" =>
                StatusCode(StatusCodes.Status403Forbidden, new { result.ErrorMessage }),
            "EXHIBITION_HAS_DEPENDENCIES" or "EXHIBITOR_DELETE_CONFLICT" or "STAFF_BADGE_CONFLICT" or "EXHIBITOR_HAS_INVOICES" or "EXHIBITOR_HAS_RESERVATIONS" or "VENUE_HAS_DEPENDENCIES" or
            "EXHIBITION_VENUE_LOCKED" or "EXHIBITION_DELETE_CONFLICT" or "HALL_HAS_ACTIVE_BOOTHS" or "HALL_HAS_DEPENDENCIES" or
            "HALL_DELETE_CONFLICT" or "TEMPLATE_CONFLICT" or "BOOTH_GEOMETRY_LOCKED" or "LAST_TENANT_ADMIN" =>
                Conflict(new { result.ErrorMessage, result.ErrorCode }),
            "RESERVATION_CONFLICT" or "PAYMENT_CONFLICT" or "TICKET_SCAN_CONFLICT" or "INVOICE_NUMBER_CONFLICT" or
            "CREDIT_NOTE_CONFLICT" or "SPACE_ASSIGNMENT_CONFLICT" or "INVOICE_IMMUTABLE" or
            "TENANT_PROVISIONING_CONFLICT" or "INVITATION_ALREADY_USED" or "FLOOR_PLAN_CONFLICT" =>
                Conflict(new { result.ErrorMessage, result.ErrorCode }),
            "INVITATION_EXPIRED" =>
                StatusCode(StatusCodes.Status410Gone, new { result.ErrorMessage, result.ErrorCode }),
            "TENANT_CREATED_INVITATION_DELIVERY_FAILED" or "INVITATION_DELIVERY_FAILED" =>
                StatusCode(StatusCodes.Status503ServiceUnavailable, new { result.ErrorMessage, result.ErrorCode }),
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
            "VENUE_CONFLICT" or "VENUE_DELETE_CONFLICT" or "SCHEDULE_HAS_REGISTRATIONS" or "SCHEDULE_CONFLICT" or "TICKET_ISSUE_CONFLICT" or "EXHIBITION_DATES_LOCKED" or "PACKAGE_CAPACITY_CONFLICT" or
            "PACKAGE_HAS_CONTRACTS" or "SPACE_HAS_ASSIGNMENTS" or "SPONSOR_HAS_CONTRACTS" or
            "SPONSOR_DELETE_CONFLICT" or "SPONSORSHIP_CONFLICT" =>
                Conflict(new { result.ErrorMessage, result.ErrorCode }),
            var c when c != null && c.EndsWith("_NOT_FOUND") =>
                NotFound(new { result.ErrorMessage, result.ErrorCode }),
            "SUBSCRIPTION_PLAN_CHANGE_NOT_ALLOWED" =>
                StatusCode(StatusCodes.Status403Forbidden, new { result.ErrorMessage, result.ErrorCode }),
            "UNAUTHORIZED" or "SELF_MANAGEMENT_FORBIDDEN" =>
                StatusCode(StatusCodes.Status403Forbidden, new { result.ErrorMessage }),
            "EXHIBITION_HAS_DEPENDENCIES" or "EXHIBITOR_DELETE_CONFLICT" or "STAFF_BADGE_CONFLICT" or "EXHIBITOR_HAS_INVOICES" or "EXHIBITOR_HAS_RESERVATIONS" or "VENUE_HAS_DEPENDENCIES" or
            "EXHIBITION_VENUE_LOCKED" or "EXHIBITION_DELETE_CONFLICT" or "HALL_HAS_ACTIVE_BOOTHS" or "HALL_HAS_DEPENDENCIES" or
            "HALL_DELETE_CONFLICT" or "TEMPLATE_CONFLICT" or "BOOTH_GEOMETRY_LOCKED" or "LAST_TENANT_ADMIN" =>
                Conflict(new { result.ErrorMessage, result.ErrorCode }),
            "RESERVATION_CONFLICT" or "PAYMENT_CONFLICT" or "TICKET_SCAN_CONFLICT" or "INVOICE_NUMBER_CONFLICT" or
            "CREDIT_NOTE_CONFLICT" or "INVOICE_IMMUTABLE" or
            "TENANT_PROVISIONING_CONFLICT" or "INVITATION_ALREADY_USED" or "FLOOR_PLAN_CONFLICT" =>
                Conflict(new { result.ErrorMessage, result.ErrorCode }),
            "INVITATION_EXPIRED" =>
                StatusCode(StatusCodes.Status410Gone, new { result.ErrorMessage, result.ErrorCode }),
            "TENANT_CREATED_INVITATION_DELIVERY_FAILED" or "INVITATION_DELIVERY_FAILED" =>
                StatusCode(StatusCodes.Status503ServiceUnavailable, new { result.ErrorMessage, result.ErrorCode }),
            _ =>
                BadRequest(new { result.ErrorMessage, result.ErrorCode })
        };
    }
}
