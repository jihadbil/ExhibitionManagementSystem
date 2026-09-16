using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Badge;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces;

public interface IBadgeService
{
    Task<ServiceResult<IList<BadgeTemplateDto>>> GetTemplatesAsync(int tenantId, int? exhibitionId = null);
    Task<ServiceResult<BadgeTemplateDto>> GetTemplateByIdAsync(int tenantId, int templateId);
    Task<ServiceResult<BadgeTemplateDto>> CreateTemplateAsync(int tenantId, BadgeTemplateCreateDto dto);
    Task<ServiceResult<BadgeTemplateDto>> UpdateTemplateAsync(int tenantId, int templateId, BadgeTemplateUpdateDto dto);
    Task<ServiceResult> DeleteTemplateAsync(int tenantId, int templateId, string userId);

    /// <summary>
    /// استخراج حمولة الشارة الجاهزة للطباعة من رمز QR (زائر، عارض، راعي، متحدث).
    /// </summary>
    Task<ServiceResult<BadgePrintPayloadDto>> GenerateBadgePayloadByQRCodeAsync(int tenantId, string qrCode);

    /// <summary>
    /// استخراج حمولة الشارة لزائر بالمعرف ومعرف المعرض.
    /// </summary>
    Task<ServiceResult<BadgePrintPayloadDto>> GenerateBadgePayloadForVisitorAsync(int tenantId, int visitorId, int exhibitionId);

    /// <summary>
    /// استخراج حمولة الشارة لعضو طاقم جناح عارض.
    /// </summary>
    Task<ServiceResult<BadgePrintPayloadDto>> GenerateBadgePayloadForStaffAsync(int tenantId, int staffId);
}
