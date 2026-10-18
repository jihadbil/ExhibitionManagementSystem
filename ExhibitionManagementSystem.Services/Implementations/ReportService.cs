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

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            var baseCurrency = tenant?.BaseCurrency ?? "LYD";
            var invoices = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Payments).Include(i => i.CreditNotes)
                .Where(i => i.Reservation.ExhibitionID == exhibitionId &&
                    i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Cancelled).ToListAsync();
            var expenses = await _unitOfWork.Expenses.AsQueryable()
                .Where(e => e.ExhibitionID == exhibitionId).ToListAsync();
            if (invoices.Any(i => i.BaseCurrencyCode != baseCurrency) || expenses.Any(e => e.BaseCurrencyCode != baseCurrency))
                return ServiceResult<FinancialReportDto>.Failure("توجد معاملات بعملات أساسية مختلفة؛ يجب تسويتها قبل إصدار التقرير", "FINANCIAL_CURRENCY_MISMATCH");
            foreach (var expense in expenses.Where(e => e.ExchangeRateUsed <= 0))
            {
                var conversion = await _unitOfWork.ExchangeRates.ConvertWithDetailsAsync(
                    expense.CurrencyCode, baseCurrency, expense.Amount, expense.ExpenseDate);
                if (conversion == null)
                    return ServiceResult<FinancialReportDto>.Failure(
                        $"سعر الصرف التاريخي للمصروف #{expense.ExpenseID} من {expense.CurrencyCode} إلى {baseCurrency} غير متوفر", "UNCONVERTED_EXPENSE");
                expense.ExchangeRateUsed = conversion.ExchangeRate;
                expense.ExchangeRateDate = conversion.RateDate;
                expense.AmountInBaseCurrency = conversion.TargetAmount;
            }
            decimal totalRevenue = invoices.Sum(i => InvoiceFinancialSummary.Calculate(i).AdjustedBaseTotal);
            int totalVisitors = await _unitOfWork.TicketScans.AsQueryable()
                .Where(s => s.Ticket.TenantID == tenantId && s.Ticket.ExhibitionID == exhibitionId && s.Direction == ScanDirection.In)
                .Select(s => s.Ticket.VisitorID).Distinct().CountAsync();

            var venueHalls = await _unitOfWork.Halls.FindAsync(h => h.VenueID == exhibition.VenueID);
            var hallIds = venueHalls.Select(h => h.HallID).ToList();
            var totalBooths = await _unitOfWork.Booths.CountAsync(b => hallIds.Contains(b.HallID));

            var reservations = await _unitOfWork.BoothReservations.FindAsync(r => r.ExhibitionID == exhibitionId && r.Status != ReservationStatus.Cancelled);
            var totalExhibitors = reservations.Select(r => r.ExhibitorID).Distinct().Count();

            int reservedBoothsCount = 0;
            foreach (var res in reservations)
            {
                if (res.BoothID.HasValue)
                {
                    reservedBoothsCount++;
                }
                else if (res.MergeID.HasValue)
                {
                    var merge = await _unitOfWork.BoothMerges.GetByIdWithIncludesAsync(res.MergeID.Value, m => m.MergeItems);
                    if (merge != null)
                    {
                        reservedBoothsCount += merge.MergeItems.Count;
                    }
                }
            }

            decimal occupancyRate = totalBooths > 0 ? (decimal)reservedBoothsCount / totalBooths * 100 : 0m;

            decimal totalExpenses = expenses.Sum(e => e.AmountInBaseCurrency);
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
                CurrencyCode = baseCurrency,
                GeneratedAt = DateTime.UtcNow,
                GeneratedByUserId = userId,
                ReportPeriodFrom = exhibition.StartDate,
                ReportPeriodTo = exhibition.EndDate
            };

            await _unitOfWork.FinancialReports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();

            var fullReport = await _unitOfWork.FinancialReports.AsQueryable()
                .Include(r => r.Exhibition)
                .FirstOrDefaultAsync(r => r.ReportID == report.ReportID);

            var resultDto = _mapper.Map<FinancialReportDto>(fullReport ?? report);
            return ServiceResult<FinancialReportDto>.Success(resultDto);
        }

        public async Task<ServiceResult<FinancialReportDto>> GetReportByIdAsync(int tenantId, int reportId)
        {
            var report = await _unitOfWork.FinancialReports.AsQueryable()
                .Include(r => r.Exhibition)
                .FirstOrDefaultAsync(r => r.ReportID == reportId && r.TenantID == tenantId);

            if (report == null)
            {
                return ServiceResult<FinancialReportDto>.Failure("التقرير غير موجود", "REPORT_NOT_FOUND");
            }

            var dto = _mapper.Map<FinancialReportDto>(report);
            return ServiceResult<FinancialReportDto>.Success(dto);
        }
    }
}
