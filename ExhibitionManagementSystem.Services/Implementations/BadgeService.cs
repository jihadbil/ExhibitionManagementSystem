using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Badge;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.Services.Implementations;

public class BadgeService : IBadgeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BadgeService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IList<BadgeTemplateDto>>> GetTemplatesAsync(int tenantId, int? exhibitionId = null)
    {
        var query = _unitOfWork.BadgeTemplates.AsQueryable()
            .Include(t => t.Exhibition)
            .Where(t => t.TenantID == tenantId);

        if (exhibitionId.HasValue && exhibitionId.Value > 0)
        {
            query = query.Where(t => t.ExhibitionID == exhibitionId.Value || t.ExhibitionID == null);
        }

        var templates = await query.OrderBy(t => t.TargetParticipantType).ToListAsync();
        var dtos = _mapper.Map<IList<BadgeTemplateDto>>(templates);
        return ServiceResult<IList<BadgeTemplateDto>>.Success(dtos);
    }

    public async Task<ServiceResult<BadgeTemplateDto>> GetTemplateByIdAsync(int tenantId, int templateId)
    {
        var template = await _unitOfWork.BadgeTemplates.AsQueryable()
            .Include(t => t.Exhibition)
            .FirstOrDefaultAsync(t => t.TemplateID == templateId && t.TenantID == tenantId);

        if (template == null)
        {
            return ServiceResult<BadgeTemplateDto>.Failure("قالب الشارة غير موجود", "TEMPLATE_NOT_FOUND");
        }

        var dto = _mapper.Map<BadgeTemplateDto>(template);
        return ServiceResult<BadgeTemplateDto>.Success(dto);
    }

    public async Task<ServiceResult<BadgeTemplateDto>> CreateTemplateAsync(int tenantId, BadgeTemplateCreateDto dto)
    {
        if (dto.IsDefault)
        {
            // Reset existing defaults for this participant type in this tenant/exhibition
            var existingDefaults = await _unitOfWork.BadgeTemplates.FindAsync(t =>
                t.TenantID == tenantId &&
                t.ExhibitionID == dto.ExhibitionID &&
                t.TargetParticipantType == dto.TargetParticipantType &&
                t.IsDefault);

            foreach (var t in existingDefaults)
            {
                t.IsDefault = false;
                _unitOfWork.BadgeTemplates.Update(t);
            }
        }

        var template = _mapper.Map<BadgeTemplate>(dto);
        template.TenantID = tenantId;
        template.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.BadgeTemplates.AddAsync(template);
        await _unitOfWork.SaveChangesAsync();

        var fullTemplate = await _unitOfWork.BadgeTemplates.AsQueryable()
            .Include(t => t.Exhibition)
            .FirstOrDefaultAsync(t => t.TemplateID == template.TemplateID);

        var resultDto = _mapper.Map<BadgeTemplateDto>(fullTemplate ?? template);
        return ServiceResult<BadgeTemplateDto>.Success(resultDto);
    }

    public async Task<ServiceResult<BadgeTemplateDto>> UpdateTemplateAsync(int tenantId, int templateId, BadgeTemplateUpdateDto dto)
    {
        var template = await _unitOfWork.BadgeTemplates.GetByIdAsync(templateId);
        if (template == null || template.TenantID != tenantId)
        {
            return ServiceResult<BadgeTemplateDto>.Failure("قالب الشارة غير موجود", "TEMPLATE_NOT_FOUND");
        }

        if (dto.IsDefault)
        {
            var existingDefaults = await _unitOfWork.BadgeTemplates.FindAsync(t =>
                t.TenantID == tenantId &&
                t.TemplateID != templateId &&
                t.ExhibitionID == dto.ExhibitionID &&
                t.TargetParticipantType == dto.TargetParticipantType &&
                t.IsDefault);

            foreach (var t in existingDefaults)
            {
                t.IsDefault = false;
                _unitOfWork.BadgeTemplates.Update(t);
            }
        }

        _mapper.Map(dto, template);
        template.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.BadgeTemplates.Update(template);
        await _unitOfWork.SaveChangesAsync();

        var fullTemplate = await _unitOfWork.BadgeTemplates.AsQueryable()
            .Include(t => t.Exhibition)
            .FirstOrDefaultAsync(t => t.TemplateID == templateId);

        var resultDto = _mapper.Map<BadgeTemplateDto>(fullTemplate ?? template);
        return ServiceResult<BadgeTemplateDto>.Success(resultDto);
    }

    public async Task<ServiceResult> DeleteTemplateAsync(int tenantId, int templateId, string userId)
    {
        var template = await _unitOfWork.BadgeTemplates.GetByIdAsync(templateId);
        if (template == null || template.TenantID != tenantId)
        {
            return ServiceResult.Failure("قالب الشارة غير موجود", "TEMPLATE_NOT_FOUND");
        }

        await _unitOfWork.BadgeTemplates.SoftDeleteAsync(templateId, userId);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<BadgePrintPayloadDto>> GenerateBadgePayloadByQRCodeAsync(int tenantId, string qrCode)
    {
        if (string.IsNullOrWhiteSpace(qrCode))
        {
            return ServiceResult<BadgePrintPayloadDto>.Failure("رمز QR غير صالح", "INVALID_QR");
        }

        // 1. Check if it's a Visitor Ticket
        var ticket = await _unitOfWork.Tickets.AsQueryable()
            .Include(t => t.Visitor)
            .Include(t => t.Exhibition)
                .ThenInclude(e => e.Venue)
            .FirstOrDefaultAsync(t => t.QRCode == qrCode.Trim());

        if (ticket != null && ticket.Visitor.TenantID == tenantId)
        {
            return await BuildVisitorBadgePayloadAsync(tenantId, ticket.Visitor, ticket.Exhibition, ticket.QRCode);
        }

        // 2. Check if it's a Staff QR or Visitor ID pattern
        if (int.TryParse(qrCode, out var visitorId))
        {
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId);
            if (visitor != null && visitor.TenantID == tenantId)
            {
                var latestTicket = await _unitOfWork.Tickets.AsQueryable()
                    .Include(t => t.Exhibition)
                    .Where(t => t.VisitorID == visitorId)
                    .OrderByDescending(t => t.IssuedAt)
                    .FirstOrDefaultAsync();

                if (latestTicket != null)
                {
                    return await BuildVisitorBadgePayloadAsync(tenantId, visitor, latestTicket.Exhibition, latestTicket.QRCode);
                }
            }
        }

        return ServiceResult<BadgePrintPayloadDto>.Failure("لم يتم العثور على مشارك مطابق لرمز QR المدخل", "PARTICIPANT_NOT_FOUND");
    }

    public async Task<ServiceResult<BadgePrintPayloadDto>> GenerateBadgePayloadForVisitorAsync(int tenantId, int visitorId, int exhibitionId)
    {
        var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId);
        if (visitor == null || visitor.TenantID != tenantId)
        {
            return ServiceResult<BadgePrintPayloadDto>.Failure("الزائر غير موجود", "VISITOR_NOT_FOUND");
        }

        var exhibition = await _unitOfWork.Exhibitions.AsQueryable()
            .Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.ExhibitionID == exhibitionId && e.TenantID == tenantId);

        if (exhibition == null)
        {
            return ServiceResult<BadgePrintPayloadDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
        }

        var ticket = await _unitOfWork.Tickets.AsQueryable()
            .FirstOrDefaultAsync(t => t.VisitorID == visitorId && t.ExhibitionID == exhibitionId);

        string qrCode = ticket?.QRCode ?? $"{exhibitionId}-{visitorId}-{Guid.NewGuid():N}";
        return await BuildVisitorBadgePayloadAsync(tenantId, visitor, exhibition, qrCode);
    }

    public async Task<ServiceResult<BadgePrintPayloadDto>> GenerateBadgePayloadForStaffAsync(int tenantId, int staffId)
    {
        var staff = await _unitOfWork.BoothStaffs.AsQueryable()
            .Include(s => s.Reservation)
                .ThenInclude(r => r.Exhibitor)
            .Include(s => s.Reservation)
                .ThenInclude(r => r.Exhibition)
            .Include(s => s.Reservation)
                .ThenInclude(r => r.Booth)
                    .ThenInclude(b => b.Hall)
            .FirstOrDefaultAsync(s => s.StaffID == staffId);

        if (staff == null || staff.Reservation.Exhibitor.TenantID != tenantId)
        {
            return ServiceResult<BadgePrintPayloadDto>.Failure("عضو طاقم العارض غير موجود", "STAFF_NOT_FOUND");
        }

        var exhibition = staff.Reservation.Exhibition;
        var exhibitor = staff.Reservation.Exhibitor;
        var template = await _unitOfWork.BadgeTemplates.GetTemplateForParticipantAsync(
            tenantId, exhibition.ExhibitionID, ParticipantType.Exhibitor);

        var payload = new BadgePrintPayloadDto
        {
            FullName = staff.StaffName,
            CompanyName = exhibitor.CompanyName,
            JobTitle = staff.Role ?? "طاقم الجناح",
            Email = staff.Email,
            Phone = staff.Phone,
            ParticipantType = ParticipantType.Exhibitor,
            ParticipantTypeTitle = "عارض / EXHIBITOR",
            QRCode = $"STAFF-{staff.StaffID}-{staff.BadgeNumber ?? Guid.NewGuid().ToString("N")[..8]}",
            ExhibitionName = exhibition.Name,
            VenueAndHallName = staff.Reservation.Booth?.Hall?.HallName,
            BoothNumber = staff.Reservation.Booth?.BoothNumber,
            DatesText = $"{exhibition.StartDate:dd/MM/yyyy} - {exhibition.EndDate:dd/MM/yyyy}",
            HeaderBgColor = template?.HeaderBgColor ?? "#059669",
            HeaderTextColor = template?.HeaderTextColor ?? "#FFFFFF",
            AccentColor = template?.AccentColor ?? "#10B981",
            LogoUrl = template?.LogoUrl ?? exhibitor.LogoURL,
            FooterText = template?.CustomFooterText ?? "بطاقة دخول عارض معتمدة",
            WidthMm = template?.WidthMm ?? 86,
            HeightMm = template?.HeightMm ?? 120,
            Orientation = template?.Orientation ?? BadgeOrientation.Vertical,
            IssuedAt = DateTime.UtcNow
        };

        payload.ThermalZplCommand = GenerateZpl(payload);
        return ServiceResult<BadgePrintPayloadDto>.Success(payload);
    }

    private async Task<ServiceResult<BadgePrintPayloadDto>> BuildVisitorBadgePayloadAsync(
        int tenantId, Visitor visitor, Exhibition exhibition, string qrCode)
    {
        var participantType = DetermineParticipantType(visitor);
        var template = await _unitOfWork.BadgeTemplates.GetTemplateForParticipantAsync(
            tenantId, exhibition.ExhibitionID, participantType);

        var payload = new BadgePrintPayloadDto
        {
            FullName = visitor.FullName,
            CompanyName = "زائر معتمد",
            JobTitle = visitor.VisitorType ?? "زائر",
            Email = visitor.Email,
            Phone = visitor.Phone,
            ParticipantType = participantType,
            ParticipantTypeTitle = GetParticipantTypeTitle(participantType),
            QRCode = qrCode,
            ExhibitionName = exhibition.Name,
            VenueAndHallName = exhibition.Venue?.Name,
            DatesText = $"{exhibition.StartDate:dd/MM/yyyy} - {exhibition.EndDate:dd/MM/yyyy}",
            HeaderBgColor = template?.HeaderBgColor ?? GetDefaultColorForType(participantType),
            HeaderTextColor = template?.HeaderTextColor ?? "#FFFFFF",
            AccentColor = template?.AccentColor ?? GetDefaultAccentForType(participantType),
            LogoUrl = template?.LogoUrl,
            FooterText = template?.CustomFooterText ?? $"{exhibition.Name} — بطاقة دخول رسمية",
            WidthMm = template?.WidthMm ?? 86,
            HeightMm = template?.HeightMm ?? 120,
            Orientation = template?.Orientation ?? BadgeOrientation.Vertical,
            IssuedAt = DateTime.UtcNow
        };

        payload.ThermalZplCommand = GenerateZpl(payload);
        return ServiceResult<BadgePrintPayloadDto>.Success(payload);
    }

    private static ParticipantType DetermineParticipantType(Visitor visitor)
    {
        if (string.IsNullOrWhiteSpace(visitor.VisitorType))
            return ParticipantType.Visitor;

        var typeStr = visitor.VisitorType.ToLowerInvariant();
        if (typeStr.Contains("vip") || typeStr.Contains("شرف") || typeStr.Contains("رئيس") || typeStr.Contains("director") || typeStr.Contains("ceo"))
            return ParticipantType.VIP;
        if (typeStr.Contains("صحف") || typeStr.Contains("إعلام") || typeStr.Contains("press") || typeStr.Contains("media"))
            return ParticipantType.Press;
        if (typeStr.Contains("متحدث") || typeStr.Contains("speaker") || typeStr.Contains("محاضر") || typeStr.Contains("دكتور") || typeStr.Contains("dr"))
            return ParticipantType.Speaker;

        return ParticipantType.Visitor;
    }

    private static string GetParticipantTypeTitle(ParticipantType type) => type switch
    {
        ParticipantType.VIP => "VIP / كبار الشخصيات",
        ParticipantType.Speaker => "متحدث / SPEAKER",
        ParticipantType.Press => "إعلام / PRESS",
        ParticipantType.Organizer => "منظم / ORGANIZER",
        ParticipantType.Sponsor => "راعي / SPONSOR",
        ParticipantType.Exhibitor => "عارض / EXHIBITOR",
        _ => "زائر / VISITOR"
    };

    private static string GetDefaultColorForType(ParticipantType type) => type switch
    {
        ParticipantType.VIP => "#D97706",       // Gold / Amber
        ParticipantType.Speaker => "#8B5CF6",   // Purple
        ParticipantType.Press => "#DC2626",     // Red
        ParticipantType.Organizer => "#2563EB", // Blue
        ParticipantType.Sponsor => "#F59E0B",   // Warm Gold
        ParticipantType.Exhibitor => "#059669", // Green
        _ => "#6366F1"                          // Indigo (Standard Visitor)
    };

    private static string GetDefaultAccentForType(ParticipantType type) => type switch
    {
        ParticipantType.VIP => "#B45309",
        ParticipantType.Speaker => "#7C3AED",
        ParticipantType.Press => "#B91C1C",
        ParticipantType.Organizer => "#1D4ED8",
        ParticipantType.Sponsor => "#D97706",
        ParticipantType.Exhibitor => "#047857",
        _ => "#4F46E5"
    };

    /// <summary>
    /// توليد أوامر الطباعة المباشرة ZPL لطابعات Zebra الحرارية القياسية (203 DPI).
    /// </summary>
    private static string GenerateZpl(BadgePrintPayloadDto payload)
    {
        var sb = new StringBuilder();
        sb.AppendLine("^XA"); // Start Format
        sb.AppendLine("^PW600"); // Print Width (approx 3 inches at 203 dpi)
        sb.AppendLine("^LL800"); // Label Length

        // Top Header Block
        sb.AppendLine("^FO30,30^GB540,100,100^FS"); // Solid Header Bar
        sb.AppendLine($"^FO50,55^A0N,40,40^FR^FD{payload.ParticipantTypeTitle}^FS");

        // Name
        sb.AppendLine($"^FO50,160^A0N,45,45^FD{payload.FullName}^FS");

        // Company
        if (!string.IsNullOrWhiteSpace(payload.CompanyName))
        {
            sb.AppendLine($"^FO50,220^A0N,32,32^FD{payload.CompanyName}^FS");
        }

        // Job Title
        if (!string.IsNullOrWhiteSpace(payload.JobTitle))
        {
            sb.AppendLine($"^FO50,265^A0N,26,26^FD{payload.JobTitle}^FS");
        }

        // Separator Line
        sb.AppendLine("^FO50,310^GB500,2,2^FS");

        // QR Code
        sb.AppendLine("^FO180,340^BQN,2,8");
        sb.AppendLine($"^FDQA,{payload.QRCode}^FS");

        // Exhibition & Date Info
        sb.AppendLine($"^FO50,620^A0N,28,28^FB500,2,0,C^FD{payload.ExhibitionName}^FS");
        if (!string.IsNullOrWhiteSpace(payload.DatesText))
        {
            sb.AppendLine($"^FO50,690^A0N,22,22^FB500,1,0,C^FD{payload.DatesText}^FS");
        }

        sb.AppendLine("^XZ"); // End Format
        return sb.ToString();
    }
}
