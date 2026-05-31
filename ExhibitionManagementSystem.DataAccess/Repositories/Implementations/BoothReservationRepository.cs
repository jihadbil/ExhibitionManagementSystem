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
    public class BoothReservationRepository : GenericRepository<BoothReservation>, IBoothReservationRepository
    {
        public BoothReservationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<BoothReservation>> GetByExhibitionAsync(int exhibitionId)
        {
            return await FindAsync(r => r.ExhibitionID == exhibitionId);
        }

        public async Task<IReadOnlyList<BoothReservation>> GetByExhibitorAsync(int exhibitorId)
        {
            return await FindAsync(r => r.ExhibitorID == exhibitorId);
        }

        public async Task<IReadOnlyList<BoothReservation>> GetByStatusAsync(int exhibitionId, ReservationStatus status)
        {
            return await FindAsync(r => r.ExhibitionID == exhibitionId && r.Status == status);
        }

        public async Task<BoothReservation?> GetWithInvoiceAsync(int reservationId)
        {
            return await _dbSet.AsNoTracking()
                .Include(r => r.Invoice)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);
        }

        public async Task<BoothReservation?> GetWithServicesAsync(int reservationId)
        {
            return await _dbSet.AsNoTracking()
                .Include(r => r.ReservationServices)
                    .ThenInclude(rs => rs.Service)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);
        }

        public async Task<BoothReservation?> GetFullDetailAsync(int reservationId)
        {
            return await _dbSet.AsNoTracking()
                .Include(r => r.Booth)
                .Include(r => r.ReservationServices)
                    .ThenInclude(rs => rs.Service)
                .Include(r => r.Invoice)
                    .ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);
        }

        public async Task<bool> IsBoothReservedAsync(int boothId, int exhibitionId)
        {
            return await ExistsAsync(r => r.BoothID == boothId && 
                                          r.ExhibitionID == exhibitionId && 
                                          r.Status != ReservationStatus.Cancelled);
        }

        public async Task<bool> IsMergeReservedAsync(int mergeId, int exhibitionId)
        {
            return await ExistsAsync(r => r.MergeID == mergeId && 
                                          r.ExhibitionID == exhibitionId && 
                                          r.Status != ReservationStatus.Cancelled);
        }

        public async Task<decimal> GetTotalRevenueAsync(int exhibitionId)
        {
            return await _dbSet.AsNoTracking()
                .Where(r => r.ExhibitionID == exhibitionId && r.Status != ReservationStatus.Cancelled)
                .SumAsync(r => (decimal?)r.TotalAmount) ?? 0m;
        }

        public async Task<IReadOnlyList<BoothReservation>> GetUnpaidReservationsAsync(int exhibitionId)
        {
            // Unpaid means either there is no invoice, or the invoice status is not paid (excluding cancelled reservations)
            return await _dbSet.AsNoTracking()
                .Include(r => r.Invoice)
                .Where(r => r.ExhibitionID == exhibitionId && 
                            r.Status != ReservationStatus.Cancelled && 
                            (r.Invoice == null || r.Invoice.Status != InvoiceStatus.Paid))
                .ToListAsync();
        }
    }
}
