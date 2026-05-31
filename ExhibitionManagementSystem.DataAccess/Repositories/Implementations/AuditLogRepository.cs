using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<AuditLog>> GetByTableAsync(int tenantId, string tableName)
        {
            return await FindAsync(a => a.TenantID == tenantId && a.TableName == tableName);
        }

        public async Task<IReadOnlyList<AuditLog>> GetByRecordAsync(int tenantId, string tableName, string recordId)
        {
            return await FindAsync(a => a.TenantID == tenantId && a.TableName == tableName && a.RecordID == recordId);
        }

        public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(int tenantId, string userId)
        {
            return await FindAsync(a => a.TenantID == tenantId && a.UserId == userId);
        }

        public async Task<IReadOnlyList<AuditLog>> GetByDateRangeAsync(int tenantId, DateTime from, DateTime to)
        {
            return await FindAsync(a => a.TenantID == tenantId && a.ActionAt >= from && a.ActionAt <= to);
        }

        public async Task<int> DeleteOlderThanAsync(int tenantId, DateTime cutoffDate)
        {
            return await _dbSet
                .Where(a => a.TenantID == tenantId && a.ActionAt < cutoffDate)
                .ExecuteDeleteAsync();
        }
    }
}
