using System.Collections.Generic;
using System.Threading.Tasks;
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
    Task<ServiceResult<BadgePrintPayloadDto>> GenerateBadgePayloadByQRCodeAsync(int tenantId, string qrCode);
    Task<ServiceResult<BadgePrintPayloadDto>> GenerateBadgePayloadForVisitorAsync(int tenantId, int visitorId, int exhibitionId);
    Task<ServiceResult<BadgePrintPayloadDto>> GenerateBadgePayloadForStaffAsync(int tenantId, int staffId);
}
