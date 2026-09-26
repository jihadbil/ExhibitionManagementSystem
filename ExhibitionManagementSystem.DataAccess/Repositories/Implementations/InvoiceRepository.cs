using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.DataAccess.Repositories;
using System.Data;

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
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId);
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
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;
            if (shouldClose)
            {
                await connection.OpenAsync();
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
                command.CommandText =
                    """
                    MERGE InvoiceSequences WITH (HOLDLOCK) AS target
                    USING (SELECT @tenantId AS TenantID, @year AS [Year]) AS source
                        ON target.TenantID = source.TenantID
                       AND target.[Year] = source.[Year]
                    WHEN MATCHED THEN
                        UPDATE SET LastValue = target.LastValue + 1
                    WHEN NOT MATCHED THEN
                        INSERT (TenantID, [Year], LastValue)
                        VALUES (source.TenantID, source.[Year], 1)
                    OUTPUT inserted.LastValue;
                    """;

                var tenantParameter = command.CreateParameter();
                tenantParameter.ParameterName = "@tenantId";
                tenantParameter.DbType = DbType.Int32;
                tenantParameter.Value = tenantId;
                command.Parameters.Add(tenantParameter);

                var yearParameter = command.CreateParameter();
                yearParameter.ParameterName = "@year";
                yearParameter.DbType = DbType.Int32;
                yearParameter.Value = year;
                command.Parameters.Add(yearParameter);

                var result = await command.ExecuteScalarAsync();
                var sequence = Convert.ToInt32(result);
                return InvoiceNumberFormatter.Format(tenantId, year, sequence);
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
