using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class TicketService : ITicketService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TicketService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<TicketDto>> IssueTicketAsync(int tenantId, TicketCreateDto dto)
        {
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(dto.VisitorID);
            if (visitor == null || visitor.TenantID != tenantId)
            {
                return ServiceResult<TicketDto>.Failure("الزائر غير موجود", "VISITOR_NOT_FOUND");
            }

            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<TicketDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            string qrCode = $"{dto.ExhibitionID}-{dto.VisitorID}-{Guid.NewGuid():N}";
            bool isUnique = await _unitOfWork.Tickets.IsQRCodeUniqueAsync(qrCode);
            while (!isUnique)
            {
                qrCode = $"{dto.ExhibitionID}-{dto.VisitorID}-{Guid.NewGuid():N}";
                isUnique = await _unitOfWork.Tickets.IsQRCodeUniqueAsync(qrCode);
            }

            var ticket = _mapper.Map<Ticket>(dto);
            ticket.QRCode = qrCode;
            ticket.Status = TicketStatus.Active;
            ticket.IssuedAt = DateTime.UtcNow;

            await _unitOfWork.Tickets.AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            var fullTicket = await _unitOfWork.Tickets.AsQueryable()
                .Include(t => t.Visitor)
                .Include(t => t.Exhibition)
                .Include(t => t.Currency)
                .Include(t => t.TicketScans)
                .FirstOrDefaultAsync(t => t.TicketID == ticket.TicketID);

            var resultDto = _mapper.Map<TicketDto>(fullTicket ?? ticket);
            return ServiceResult<TicketDto>.Success(resultDto);
        }

        public async Task<ServiceResult<IList<TicketDto>>> GetByVisitorAsync(int tenantId, int visitorId)
        {
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId);
            if (visitor == null || visitor.TenantID != tenantId)
            {
                return ServiceResult<IList<TicketDto>>.Failure("الزائر غير موجود", "VISITOR_NOT_FOUND");
            }

            var tickets = await _unitOfWork.Tickets.AsQueryable()
                .Include(t => t.Visitor)
                .Include(t => t.Exhibition)
                .Include(t => t.Currency)
                .Include(t => t.TicketScans)
                .Where(t => t.VisitorID == visitorId)
                .ToListAsync();

            var dtos = _mapper.Map<IList<TicketDto>>(tickets);
            return ServiceResult<IList<TicketDto>>.Success(dtos);
        }

        public async Task<ServiceResult<IList<TicketDto>>> GetByExhibitionAsync(int tenantId, int exhibitionId)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(exhibitionId);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<IList<TicketDto>>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            var tickets = await _unitOfWork.Tickets.AsQueryable()
                .Include(t => t.Visitor)
                .Include(t => t.Exhibition)
                .Include(t => t.Currency)
                .Include(t => t.TicketScans)
                .Where(t => t.ExhibitionID == exhibitionId)
                .ToListAsync();

            var dtos = _mapper.Map<IList<TicketDto>>(tickets);
            return ServiceResult<IList<TicketDto>>.Success(dtos);
        }

        public async Task<ServiceResult<TicketScanDto>> ScanTicketAsync(int tenantId, string qrCode, string direction, string? location, string scannedByUserId)
        {
            var ticket = await _unitOfWork.Tickets.AsQueryable()
                .Include(t => t.Visitor)
                .Include(t => t.Exhibition)
                .Include(t => t.TicketScans)
                .FirstOrDefaultAsync(t => t.QRCode == qrCode);

            if (ticket == null || ticket.Visitor.TenantID != tenantId)
            {
                return ServiceResult<TicketScanDto>.Failure("التذكرة غير موجودة", "TICKET_NOT_FOUND");
            }

            if (ticket.Status != TicketStatus.Active && ticket.Status != TicketStatus.Used)
            {
                return ServiceResult<TicketScanDto>.Failure("التذكرة ملغاة أو غير صالحة حالياً", "TICKET_NOT_VALID");
            }

            if (ticket.ValidDate.HasValue && ticket.ValidDate.Value.Date < DateTime.UtcNow.Date)
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    ticket.Status = TicketStatus.Expired;
                    _unitOfWork.Tickets.Update(ticket);
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }
                return ServiceResult<TicketScanDto>.Failure("التذكرة منتهية الصلاحية", "TICKET_EXPIRED");
            }

            if (!Enum.TryParse<ScanDirection>(direction, true, out var scanDirection))
            {
                return ServiceResult<TicketScanDto>.Failure("اتجاه المسح غير صالح", "INVALID_SCAN_DIRECTION");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var scan = new TicketScan
                {
                    TicketID = ticket.TicketID,
                    ScanDateTime = DateTime.UtcNow,
                    GateName = location ?? "B1 Gate",
                    Direction = scanDirection,
                    ScannedByUserId = scannedByUserId
                };

                if (ticket.Status == TicketStatus.Active && scanDirection == ScanDirection.In)
                {
                    ticket.Status = TicketStatus.Used;
                    _unitOfWork.Tickets.Update(ticket);
                }

                await _unitOfWork.TicketScans.AddAsync(scan);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var fullScan = await _unitOfWork.TicketScans.AsQueryable()
                    .Include(s => s.Ticket)
                    .FirstOrDefaultAsync(s => s.ScanID == scan.ScanID);

                var resultDto = _mapper.Map<TicketScanDto>(fullScan ?? scan);
                return ServiceResult<TicketScanDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<TicketScanDto>.Failure($"فشل تسجيل مسح التذكرة: {ex.Message}", "SCAN_FAILED");
            }
        }

        public async Task<ServiceResult<IList<TicketScanDto>>> GetScanHistoryAsync(int tenantId, int ticketId)
        {
            var ticket = await _unitOfWork.Tickets.AsQueryable()
                .Include(t => t.Visitor)
                .FirstOrDefaultAsync(t => t.TicketID == ticketId);

            if (ticket == null || ticket.Visitor.TenantID != tenantId)
            {
                return ServiceResult<IList<TicketScanDto>>.Failure("التذكرة غير موجودة", "TICKET_NOT_FOUND");
            }

            var scans = await _unitOfWork.TicketScans.AsQueryable()
                .Include(s => s.Ticket)
                .Where(s => s.TicketID == ticketId)
                .OrderByDescending(s => s.ScanDateTime)
                .ToListAsync();

            var dtos = _mapper.Map<IList<TicketScanDto>>(scans);
            return ServiceResult<IList<TicketScanDto>>.Success(dtos);
        }
    }
}
