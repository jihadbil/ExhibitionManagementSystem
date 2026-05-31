using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<IReadOnlyList<Payment>> GetByInvoiceAsync(int invoiceId);
        Task<decimal> GetTotalPaidAsync(int invoiceId);
        Task<IReadOnlyList<Payment>> GetByDateRangeAsync(int tenantId, DateTime from, DateTime to);
    }
}
