using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class InvoiceRepository : GenericRepository<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Invoice?> GetByReservationAsync(int reservationId)
        {
            return await FirstOrDefaultAsync(i => i.ReservationID == reservationId);
        }

        public async Task<Invoice?> GetByNumberAsync(int tenantId, string invoiceNumber)
        {
            return await FirstOrDefaultAsync(i => i.TenantID == tenantId && i.InvoiceNumber == invoiceNumber);
        }

        public async Task<IReadOnlyList<Invoice>> GetByStatusAsync(int tenantId, InvoiceStatus status)
        {
            return await FindAsync(i => i.TenantID == tenantId && i.Status == status);
        }

        public async Task<Invoice?> GetWithPaymentsAsync(int invoiceId)
        {
            return await _dbSet.AsNoTracking()
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId, cancellationToken: _context.RequestCancellation);
        }

        public async Task<IReadOnlyList<Invoice>> GetOverdueInvoicesAsync(int tenantId)
        {
            return await FindAsync(i => i.TenantID == tenantId && 
                                        i.DueDate < DateTime.UtcNow && 
                                        i.Status != InvoiceStatus.Paid && 
                                        i.Status != InvoiceStatus.Cancelled);
        }

        public async Task<string> GenerateNextInvoiceNumberAsync(int tenantId)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"INV-{tenantId:D4}-{year}-";

            // SQL sequences allocate atomically across requests and instances.
            // Gaps on rollback are intentional; these are unique document IDs.
            var values = await _context.Database.SqlQueryRaw<long>(
                "SELECT NEXT VALUE FOR [InvoiceNumbers] AS [Value]").ToListAsync(cancellationToken: _context.RequestCancellation);
            return $"{prefix}{values[0]:D10}";
        }
    }
}
