using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Badge;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Badges;

public class BadgeApiClient : ApiClientBase, IBadgeService
{
    public BadgeApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<IList<BadgeTemplateDto>>> GetTemplatesAsync(int tenantId, int? exhibitionId = null)
    {
        var url = exhibitionId.HasValue ? $"api/badges/templates?exhibitionId={exhibitionId.Value}" : "api/badges/templates";
        return GetAsync<IList<BadgeTemplateDto>>(url);
    }

    public Task<ServiceResult<BadgeTemplateDto>> GetTemplateByIdAsync(int tenantId, int templateId)
    {
        return GetAsync<BadgeTemplateDto>($"api/badges/templates/{templateId}");
    }

    public Task<ServiceResult<BadgeTemplateDto>> CreateTemplateAsync(int tenantId, BadgeTemplateCreateDto dto)
    {
        return PostAsync<BadgeTemplateDto>("api/badges/templates", dto);
    }

    public Task<ServiceResult<BadgeTemplateDto>> UpdateTemplateAsync(int tenantId, int templateId, BadgeTemplateUpdateDto dto)
    {
        return PutAsync<BadgeTemplateDto>($"api/badges/templates/{templateId}", dto);
    }

    public Task<ServiceResult> DeleteTemplateAsync(int tenantId, int templateId, string userId)
    {
        return DeleteAsync($"api/badges/templates/{templateId}");
    }

    public Task<ServiceResult<BadgePrintPayloadDto>> GenerateBadgePayloadByQRCodeAsync(int tenantId, string qrCode)
    {
        return GetAsync<BadgePrintPayloadDto>($"api/badges/render-by-qr/{qrCode}");
    }

    public Task<ServiceResult<BadgePrintPayloadDto>> GenerateBadgePayloadForVisitorAsync(int tenantId, int visitorId, int exhibitionId)
    {
        return GetAsync<BadgePrintPayloadDto>($"api/badges/render-visitor/{visitorId}/{exhibitionId}");
    }

    public Task<ServiceResult<BadgePrintPayloadDto>> GenerateBadgePayloadForStaffAsync(int tenantId, int staffId)
    {
        return GetAsync<BadgePrintPayloadDto>($"api/badges/render-staff/{staffId}");
    }
}
