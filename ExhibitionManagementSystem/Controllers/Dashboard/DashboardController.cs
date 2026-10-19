using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.Models.DTOs.Dashboard;
using ExhibitionManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Controllers.Dashboard
{
    [Route("api/[controller]")]
    public class DashboardController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public DashboardController(ApplicationDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        // GET /api/Dashboard/stats
        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetStats()
        {
            var tenantId = TenantId;
            var tenant = await _context.Tenants
                .Include(t => t.Currency)
                .FirstOrDefaultAsync(t => t.TenantID == tenantId, cancellationToken: _context.RequestCancellation);

            var totalExhibitions = await _context.Exhibitions.CountAsync(e => e.TenantID == tenantId, cancellationToken: _context.RequestCancellation);
            var activeExhibitions = await _context.Exhibitions.CountAsync(e => e.TenantID == tenantId && e.Status == ExhibitionStatus.Open, cancellationToken: _context.RequestCancellation);
            var totalExhibitors = await _context.Exhibitors.CountAsync(e => e.TenantID == tenantId, cancellationToken: _context.RequestCancellation);
            var totalVisitors = await _context.Visitors.CountAsync(v => v.TenantID == tenantId, cancellationToken: _context.RequestCancellation);

            // Booth calculations
            var totalBooths = await _context.Booths.CountAsync(b => b.Hall.Venue.TenantID == tenantId, cancellationToken: _context.RequestCancellation);
            var occupiedBooths = await _context.Booths.CountAsync(b => b.Hall.Venue.TenantID == tenantId && b.Status == BoothStatus.Reserved, cancellationToken: _context.RequestCancellation);
            double occupancyRate = totalBooths > 0 ? ((double)occupiedBooths / totalBooths) * 100 : 0;

            var pendingReservations = await _context.BoothReservations.CountAsync(r => r.Exhibition.TenantID == tenantId && r.Status == ReservationStatus.Pending, cancellationToken: _context.RequestCancellation);

            // Financials
            var invoices = await _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.TenantID == tenantId)
                .ToListAsync(cancellationToken: _context.RequestCancellation);
            var currency = tenant?.BaseCurrency ?? "USD";
            var paid = await CurrencyTotals.SumAsync(_unitOfWork,
                invoices.SelectMany(i => i.Payments).Where(p => p.Status == PaymentStatus.Completed).Select(p => (p.Amount, p.CurrencyCode)), currency);
            var pending = await CurrencyTotals.SumAsync(_unitOfWork,
                invoices.Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
                    .Select(i => (Math.Max(0, i.TotalAmount - i.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount)), i.CurrencyCode)), currency);
            if (!paid.IsSuccess || !pending.IsSuccess)
                return BadRequest(new { ErrorCode = "EXCHANGE_RATE_NOT_FOUND", ErrorMessage = "سعر صرف مطلوب للإحصائيات غير متوفر" });

            return Ok(new DashboardStatsDto
            {
                TotalExhibitions = totalExhibitions,
                ActiveExhibitions = activeExhibitions,
                TotalExhibitors = totalExhibitors,
                TotalVisitors = totalVisitors,
                TotalRevenue = (double)paid.Data,
                PendingRevenue = (double)pending.Data,
                TotalBooths = totalBooths,
                OccupiedBooths = occupiedBooths,
                OccupancyRate = occupancyRate,
                PendingReservations = pendingReservations,
                CurrencyCode = tenant?.BaseCurrency ?? "USD",
                CurrencySymbol = tenant?.Currency?.Symbol ?? "$"
            });
        }

        // GET /api/Dashboard/recent-activity?limit=8
        [HttpGet("recent-activity")]
        public async Task<ActionResult<IList<ActivityItemDto>>> GetRecentActivity([FromQuery] int limit = 8)
        {
            var tenantId = TenantId;
            var logs = await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.TenantID == tenantId)
                .OrderByDescending(a => a.ActionAt)
                .Take(limit)
                .ToListAsync(cancellationToken: _context.RequestCancellation);

            var activities = logs.Select(log => new ActivityItemDto
            {
                ActivityId = (int)log.LogID,
                Type = log.Action,
                Description = $"{log.Action} on {log.TableName} (ID: {log.RecordID})",
                EntityType = log.TableName,
                EntityId = int.TryParse(log.RecordID, out var id) ? id : 0,
                EntityName = log.TableName,
                UserId = log.UserId,
                UserName = log.User?.FullName ?? "System User",
                Timestamp = log.ActionAt
            }).ToList();

            return Ok(activities);
        }

        // GET /api/Dashboard/revenue-chart?period=6m
        [HttpGet("revenue-chart")]
        public async Task<ActionResult<IList<RevenueChartPointDto>>> GetRevenueChart([FromQuery] string period = "6m")
        {
            var tenantId = TenantId;
            var monthsCount = 6;
            if (period == "12m") monthsCount = 12;

            var startDate = DateTime.UtcNow.AddMonths(-monthsCount);

            var invoices = await _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.TenantID == tenantId && (i.InvoiceDate >= startDate || i.Payments.Any(p => p.PaymentDate >= startDate)))
                .ToListAsync(cancellationToken: _context.RequestCancellation);
            var currency = await _context.Tenants.Where(t => t.TenantID == tenantId).Select(t => t.BaseCurrency).SingleAsync(cancellationToken: _context.RequestCancellation);

            var chartPoints = new List<RevenueChartPointDto>();

            for (int i = monthsCount - 1; i >= 0; i--)
            {
                var monthDate = DateTime.UtcNow.AddMonths(-i);
                var label = monthDate.ToString("MMMM"); // e.g. "June"

                var monthlyInvoices = invoices.Where(inv => inv.InvoiceDate.Year == monthDate.Year && inv.InvoiceDate.Month == monthDate.Month
                    && inv.Status != InvoiceStatus.Cancelled && inv.Status != InvoiceStatus.Draft).ToList();
                var revenue = await CurrencyTotals.SumAsync(_unitOfWork, monthlyInvoices.Select(inv => (inv.TotalAmount, inv.CurrencyCode)), currency);
                var paid = await CurrencyTotals.SumAsync(_unitOfWork,
                    invoices.SelectMany(inv => inv.Payments).Where(p => p.Status == PaymentStatus.Completed
                        && p.PaymentDate.Year == monthDate.Year && p.PaymentDate.Month == monthDate.Month).Select(p => (p.Amount, p.CurrencyCode)), currency);
                var pending = await CurrencyTotals.SumAsync(_unitOfWork, monthlyInvoices.Select(inv =>
                    (Math.Max(0, inv.TotalAmount - inv.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount)), inv.CurrencyCode)), currency);
                if (!revenue.IsSuccess || !paid.IsSuccess || !pending.IsSuccess)
                    return BadRequest(new { ErrorCode = "EXCHANGE_RATE_NOT_FOUND", ErrorMessage = "سعر صرف مطلوب للرسم غير متوفر" });

                chartPoints.Add(new RevenueChartPointDto
                {
                    Label = label,
                    Month = label,
                    Revenue = (double)revenue.Data,
                    Paid = (double)paid.Data,
                    Pending = (double)pending.Data
                });
            }

            return Ok(chartPoints);
        }
    }
}
