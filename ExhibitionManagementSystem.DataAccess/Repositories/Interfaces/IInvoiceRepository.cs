using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IInvoiceRepository : IGenericRepository<Invoice>
    {
        Task<Invoice?> GetByReservationAsync(int reservationId);
        Task<Invoice?> GetByNumberAsync(int tenantId, string invoiceNumber);
        Task<IReadOnlyList<Invoice>> GetByStatusAsync(int tenantId, InvoiceStatus status);
        Task<Invoice?> GetWithPaymentsAsync(int invoiceId);
        Task<IReadOnlyList<Invoice>> GetOverdueInvoicesAsync(int tenantId);
        Task<string> GenerateNextInvoiceNumberAsync(int tenantId);
    }
}
