using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReportService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<FinancialReportDto>> GenerateExhibitionReportAsync(int tenantId, int exhibitionId, string userId)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(exhibitionId);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<FinancialReportDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            var reportCurrency = string.IsNullOrWhiteSpace(exhibition.EntryCurrency) ? "USD" : exhibition.EntryCurrency;
            var revenueGroups = await _unitOfWork.Invoices.AsQueryable()
                .Where(i => i.TenantID == tenantId && i.Reservation.ExhibitionID == exhibitionId
                    && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
                .GroupBy(i => i.CurrencyCode)
                .Select(g => new { Currency = g.Key, Amount = g.Sum(i => i.TotalAmount) }).ToListAsync(cancellationToken: _unitOfWork.RequestCancellation);
            var revenue = await CurrencyTotals.SumAsync(_unitOfWork, revenueGroups.Select(g => (g.Amount, g.Currency)), reportCurrency);
            if (!revenue.IsSuccess)
                return ServiceResult<FinancialReportDto>.Failure(revenue.ErrorMessage!, revenue.ErrorCode);
            decimal totalRevenue = revenue.Data;
            int totalVisitors = await _unitOfWork.Tickets.AsQueryable()
                .Where(t => t.ExhibitionID == exhibitionId && (t.Status == TicketStatus.Active || t.Status == TicketStatus.Used))
                .Select(t => t.VisitorID).Distinct().CountAsync(cancellationToken: _unitOfWork.RequestCancellation);

            var venueHalls = await _unitOfWork.Halls.FindAsync(h => h.VenueID == exhibition.VenueID);
            var hallIds = venueHalls.Select(h => h.HallID).ToList();
            var totalBooths = await _unitOfWork.Booths.CountAsync(b => hallIds.Contains(b.HallID), cancellationToken: _unitOfWork.RequestCancellation);

            var reservations = await _unitOfWork.BoothReservations.FindAsync(r => r.ExhibitionID == exhibitionId && r.Status != ReservationStatus.Cancelled);
            var totalExhibitors = reservations.Select(r => r.ExhibitorID).Distinct().Count();

            int reservedBoothsCount = 0;
            var mergeIds = reservations.Where(r => r.MergeID.HasValue).Select(r => r.MergeID!.Value).Distinct().ToList();
            var mergeCounts = await _unitOfWork.BoothMerges.AsQueryable().Where(m => mergeIds.Contains(m.MergeID))
                .Select(m => new { m.MergeID, Count = m.MergeItems.Count }).ToDictionaryAsync(m => m.MergeID, m => m.Count);
            foreach (var res in reservations)
            {
                if (res.BoothID.HasValue)
                {
                    reservedBoothsCount++;
                }
                else if (res.MergeID.HasValue)
                {
                    reservedBoothsCount += mergeCounts.GetValueOrDefault(res.MergeID.Value);
                }
            }

            decimal occupancyRate = totalBooths > 0 ? (decimal)reservedBoothsCount / totalBooths * 100 : 0m;

            var expenseGroups = await _unitOfWork.Expenses.AsQueryable().Where(e => e.ExhibitionID == exhibitionId)
                .GroupBy(e => e.CurrencyCode).Select(g => new { Currency = g.Key, Amount = g.Sum(e => e.Amount) }).ToListAsync(cancellationToken: _unitOfWork.RequestCancellation);
            var expenses = await CurrencyTotals.SumAsync(_unitOfWork, expenseGroups.Select(g => (g.Amount, g.Currency)), reportCurrency);
            if (!expenses.IsSuccess)
                return ServiceResult<FinancialReportDto>.Failure(expenses.ErrorMessage!, expenses.ErrorCode);
            decimal totalExpenses = expenses.Data;
            decimal netProfit = totalRevenue - totalExpenses;

            var report = new FinancialReport
            {
                TenantID = tenantId,
                ExhibitionID = exhibitionId,
                TotalRevenue = totalRevenue,
                TotalExpenses = totalExpenses,
                NetProfit = netProfit,
                TotalVisitors = totalVisitors,
                TotalExhibitors = totalExhibitors,
                TotalBooths = totalBooths,
                OccupancyRate = occupancyRate,
                CurrencyCode = reportCurrency,
                GeneratedAt = DateTime.UtcNow,
                GeneratedByUserId = userId,
                ReportPeriodFrom = exhibition.StartDate,
                ReportPeriodTo = exhibition.EndDate
            };

            await _unitOfWork.FinancialReports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();

            var fullReport = await _unitOfWork.FinancialReports.AsQueryable()
                .Include(r => r.Exhibition)
                .FirstOrDefaultAsync(r => r.ReportID == report.ReportID, cancellationToken: _unitOfWork.RequestCancellation);

            var resultDto = _mapper.Map<FinancialReportDto>(fullReport ?? report);
            return ServiceResult<FinancialReportDto>.Success(resultDto);
        }

        public async Task<ServiceResult<FinancialReportDto>> GetReportByIdAsync(int tenantId, int reportId)
        {
            var report = await _unitOfWork.FinancialReports.AsQueryable()
                .Include(r => r.Exhibition)
                .FirstOrDefaultAsync(r => r.ReportID == reportId && r.TenantID == tenantId, cancellationToken: _unitOfWork.RequestCancellation);

            if (report == null)
            {
                return ServiceResult<FinancialReportDto>.Failure("التقرير غير موجود", "REPORT_NOT_FOUND");
            }

            var dto = _mapper.Map<FinancialReportDto>(report);
            return ServiceResult<FinancialReportDto>.Success(dto);
        }
    }
}
