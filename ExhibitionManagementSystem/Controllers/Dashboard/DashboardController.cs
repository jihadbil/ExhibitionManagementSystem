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
            var occupiedBooths = await _context.Booths.CountAsync(b => b.Hall.Venue.TenantID == tenantId && b.Status == BoothStatus.Reserved);
            double occupancyRate = totalBooths > 0 ? ((double)occupiedBooths / totalBooths) * 100 : 0;

            var pendingReservations = await _context.BoothReservations.CountAsync(r => r.Exhibition.TenantID == tenantId && r.Status == ReservationStatus.Pending);

            // Financials
            var invoices = await _context.Invoices
                .Where(i => i.TenantID == tenantId)
                .ToListAsync();

            double totalRevenue = (double)invoices.Where(i => i.Status == InvoiceStatus.Paid || i.Status == InvoiceStatus.PartiallyPaid).Sum(i => i.TotalAmount);

            // Since there is a PaidAmount/payments check
            double totalPaid = (double)await _context.Payments
                .Where(p => p.Invoice.TenantID == tenantId && p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);

            double totalPending = (double)invoices
                .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
                .Sum(i => i.TotalAmount) - totalPaid;

            if (totalPending < 0) totalPending = 0;

            return Ok(new DashboardStatsDto
            {
                TotalExhibitions = totalExhibitions,
                ActiveExhibitions = activeExhibitions,
                TotalExhibitors = totalExhibitors,
                TotalVisitors = totalVisitors,
                TotalRevenue = totalPaid > 0 ? totalPaid : totalRevenue, // use actual paid amount from payments if available
                PendingRevenue = totalPending,
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
        [HttpGet("revenue-chart")]
        public async Task<ActionResult<IList<RevenueChartPointDto>>> GetRevenueChart([FromQuery] string period = "6m")
        {
            var tenantId = TenantId;
            var monthsCount = 6;
            if (period == "12m") monthsCount = 12;

            var startDate = DateTime.UtcNow.AddMonths(-monthsCount);

            var invoices = await _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.TenantID == tenantId && i.CreatedAt >= startDate)
                .ToListAsync();

            var chartPoints = new List<RevenueChartPointDto>();

            for (int i = monthsCount - 1; i >= 0; i--)
            {
                var monthDate = DateTime.UtcNow.AddMonths(-i);
                var label = monthDate.ToString("MMMM"); // e.g. "June"

                var monthlyInvoices = invoices.Where(inv => inv.CreatedAt.Year == monthDate.Year && inv.CreatedAt.Month == monthDate.Month).ToList();

                double revenue = (double)monthlyInvoices.Sum(inv => inv.TotalAmount);
                double paid = (double)monthlyInvoices.SelectMany(inv => inv.Payments).Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
                double pending = revenue - paid;
                if (pending < 0) pending = 0;

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
