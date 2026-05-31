using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IAuditLogRepository : IGenericRepository<AuditLog>
    {
        Task<IReadOnlyList<AuditLog>> GetByTableAsync(int tenantId, string tableName);
        Task<IReadOnlyList<AuditLog>> GetByRecordAsync(int tenantId, string tableName, string recordId);
        Task<IReadOnlyList<AuditLog>> GetByUserAsync(int tenantId, string userId);
        Task<IReadOnlyList<AuditLog>> GetByDateRangeAsync(int tenantId, DateTime from, DateTime to);
        Task<int> DeleteOlderThanAsync(int tenantId, DateTime cutoffDate);
    }
}
