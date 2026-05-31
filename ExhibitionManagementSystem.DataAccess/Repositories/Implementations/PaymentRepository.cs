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
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Payment>> GetByInvoiceAsync(int invoiceId)
        {
            return await FindAsync(p => p.InvoiceID == invoiceId);
        }

        public async Task<decimal> GetTotalPaidAsync(int invoiceId)
        {
            return await _dbSet.AsNoTracking()
                .Where(p => p.InvoiceID == invoiceId && p.Status == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        }

        public async Task<IReadOnlyList<Payment>> GetByDateRangeAsync(int tenantId, DateTime from, DateTime to)
        {
            return await _dbSet.AsNoTracking()
                .Include(p => p.Invoice)
                .Where(p => p.Invoice.TenantID == tenantId && p.PaymentDate >= from && p.PaymentDate <= to)
                .ToListAsync();
        }
    }
}
