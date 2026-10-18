using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Dashboard;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Controllers.Dashboard
{
    [Route("api/[controller]")]
    public class DashboardController : BaseApiController
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/Dashboard/stats
        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetStats()
        {
            var tenantId = TenantId;
            var tenant = await _context.Tenants
                .Include(t => t.Currency)
                .FirstOrDefaultAsync(t => t.TenantID == tenantId);

            var totalExhibitions = await _context.Exhibitions.CountAsync(e => e.TenantID == tenantId);
            var activeExhibitions = await _context.Exhibitions.CountAsync(e => e.TenantID == tenantId && e.Status == ExhibitionStatus.Open);
            var totalExhibitors = await _context.Exhibitors.CountAsync(e => e.TenantID == tenantId);
            var totalVisitors = await _context.Visitors.CountAsync(v => v.TenantID == tenantId);

            // Booth calculations
            var totalBooths = await _context.Booths.CountAsync(b => b.Hall.Venue.TenantID == tenantId);
            var activeReservations = _context.BoothReservations.Where(r => r.TenantID == tenantId &&
                r.Exhibition.Status == ExhibitionStatus.Open && r.Status != ReservationStatus.Cancelled);
            var occupiedIds = await activeReservations.Where(r => r.BoothID.HasValue)
                .Select(r => r.BoothID!.Value).ToListAsync();
            var activeMergeIds = await activeReservations.Where(r => r.MergeID.HasValue)
                .Select(r => r.MergeID!.Value).ToListAsync();
            occupiedIds.AddRange(await _context.BoothMergeItems.Where(i => activeMergeIds.Contains(i.MergeID))
                .Select(i => i.BoothID).ToListAsync());
            var occupiedBooths = occupiedIds.Distinct().Count();
            double occupancyRate = totalBooths > 0 ? ((double)occupiedBooths / totalBooths) * 100 : 0;

            var pendingReservations = await _context.BoothReservations.CountAsync(r => r.Exhibition.TenantID == tenantId && r.Status == ReservationStatus.Pending);

            // Financials
            var canViewFinancials = User.IsInRole("Admin") || User.IsInRole("TenantAdmin") || User.IsInRole("Finance");
            var invoices = canViewFinancials ? await _context.Invoices
                .Include(i => i.Payments)
                .Include(i => i.CreditNotes)
                .Where(i => i.TenantID == tenantId)
                .ToListAsync() : new List<Invoice>();
            var eligibleInvoices = invoices.Where(i => i.Status is not InvoiceStatus.Draft and not InvoiceStatus.Cancelled).ToList();
            if (eligibleInvoices.Any(i => i.BaseCurrencyCode != (tenant?.BaseCurrency ?? "LYD")))
                return Conflict(new { ErrorMessage = "توجد معاملات بعملات أساسية مختلفة تحتاج تسوية", ErrorCode = "FINANCIAL_CURRENCY_MISMATCH" });
            var balances = eligibleInvoices.Select(InvoiceFinancialSummary.Calculate).ToList();
            double totalPaid = (double)balances.Sum(b => b.NetBasePaid);
            double totalPending = (double)balances.Sum(b => b.BaseRemaining);

            return Ok(new DashboardStatsDto
            {
                TotalExhibitions = totalExhibitions,
                ActiveExhibitions = activeExhibitions,
                TotalExhibitors = totalExhibitors,
                TotalVisitors = totalVisitors,
                TotalRevenue = canViewFinancials ? totalPaid : null,
                PendingRevenue = canViewFinancials ? totalPending : null,
                CanViewFinancials = canViewFinancials,
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
                .ToListAsync();

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
        [HttpGet("visitor-traffic")]
        [Authorize(Policy = AuthorizationPolicies.FinanceManagement)]
        public async Task<ActionResult<IList<VisitorTrafficPointDto>>> GetVisitorTraffic([FromQuery] int exhibitionId, [FromQuery] DateTime? date = null)
        {
            if (!await _context.Exhibitions.AnyAsync(e => e.ExhibitionID == exhibitionId && e.TenantID == TenantId))
                return NotFound(new { ErrorMessage = "المعرض غير موجود", ErrorCode = "EXHIBITION_NOT_FOUND" });
            var from = (date ?? DateTime.UtcNow).Date;
            var to = from.AddDays(1);
            var values = await _context.TicketScans.Where(s => s.Ticket.TenantID == TenantId && s.Ticket.ExhibitionID == exhibitionId
                && s.Direction == ScanDirection.In && s.ScanDateTime >= from && s.ScanDateTime < to)
                .GroupBy(s => s.ScanDateTime.Hour).Select(g => new { Hour = g.Key, Count = g.Count() }).ToListAsync();
            return Ok(Enumerable.Range(0, 24).Select(hour => new VisitorTrafficPointDto {
                Label = $"{hour:D2}:00", Value = values.FirstOrDefault(v => v.Hour == hour)?.Count ?? 0
            }).ToList());
        }

        [HttpGet("revenue-chart")]
        [Authorize(Policy = AuthorizationPolicies.FinanceManagement)]
        public async Task<ActionResult<IList<RevenueChartPointDto>>> GetRevenueChart([FromQuery] string period = "6m")
        {
            var tenantId = TenantId;
            var monthsCount = 6;
            if (period == "12m") monthsCount = 12;

            var now = DateTime.UtcNow;
            var startDate = new DateTime(now.Year, now.Month, 1).AddMonths(1 - monthsCount);
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.TenantID == tenantId);

            var invoices = await _context.Invoices
                .Include(i => i.Payments)
                .Include(i => i.CreditNotes)
                .Where(i => i.TenantID == tenantId && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Cancelled)
                .ToListAsync();
            if (invoices.Any(i => i.BaseCurrencyCode != (tenant?.BaseCurrency ?? "LYD")))
                return Conflict(new { ErrorMessage = "توجد معاملات بعملات أساسية مختلفة تحتاج تسوية", ErrorCode = "FINANCIAL_CURRENCY_MISMATCH" });

            var chartPoints = new List<RevenueChartPointDto>();

            for (int i = monthsCount - 1; i >= 0; i--)
            {
                var monthDate = startDate.AddMonths(monthsCount - 1 - i);
                var endDate = monthDate.AddMonths(1);
                var label = monthDate.ToString("MMMM"); // e.g. "June"

                var monthlyInvoices = invoices.Where(inv => inv.InvoiceDate >= monthDate && inv.InvoiceDate < endDate);
                double revenue = (double)(monthlyInvoices.Sum(inv => inv.TotalAmountInBaseCurrency) - invoices.Sum(inv =>
                    InvoiceFinancialSummary.Round(inv.CreditNotes.Where(n => n.CreatedAt >= monthDate && n.CreatedAt < endDate)
                        .Sum(n => n.Amount) * inv.ExchangeRateUsed)));
                double paid = (double)invoices.Sum(inv =>
                    inv.Payments.Where(p => p.Status == PaymentStatus.Completed && p.PaymentDate >= monthDate && p.PaymentDate < endDate)
                        .Sum(p => p.AmountInBaseCurrency) -
                    InvoiceFinancialSummary.Round(inv.CreditNotes.Where(n => n.Type == CreditNoteType.Refund &&
                        n.CreatedAt >= monthDate && n.CreatedAt < endDate).Sum(n => n.Amount) * inv.ExchangeRateUsed));
                double pending = (double)invoices.Where(inv => inv.InvoiceDate < endDate).Sum(inv =>
                    InvoiceFinancialSummary.Round(Math.Max(0,
                        inv.TotalAmount - inv.CreditNotes.Where(n => n.CreatedAt < endDate).Sum(n => n.Amount) -
                        inv.Payments.Where(p => p.Status == PaymentStatus.Completed && p.PaymentDate < endDate).Sum(p => p.Amount) +
                        inv.CreditNotes.Where(n => n.Type == CreditNoteType.Refund && n.CreatedAt < endDate).Sum(n => n.Amount))
                        * inv.ExchangeRateUsed));

                chartPoints.Add(new RevenueChartPointDto
                {
                    Label = label,
                    Month = label,
                    Revenue = revenue,
                    Paid = paid,
                    Pending = pending
                });
            }

            return Ok(chartPoints);
        }
    }
}
