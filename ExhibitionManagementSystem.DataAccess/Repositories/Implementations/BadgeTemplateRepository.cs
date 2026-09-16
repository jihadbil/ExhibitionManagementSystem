using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations;

public class BadgeTemplateRepository : GenericRepository<BadgeTemplate>, IBadgeTemplateRepository
{
    public BadgeTemplateRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<BadgeTemplate?> GetTemplateForParticipantAsync(int tenantId, int? exhibitionId, ParticipantType participantType)
    {
        // 1. Try to find an exhibition-specific template for this participant type
        if (exhibitionId.HasValue)
        {
            var exhibitionTemplate = await _dbSet
                .Where(t => t.TenantID == tenantId && t.ExhibitionID == exhibitionId.Value && t.TargetParticipantType == participantType)
                .OrderByDescending(t => t.IsDefault)
                .FirstOrDefaultAsync();

            if (exhibitionTemplate != null)
                return exhibitionTemplate;
        }

        // 2. Fallback to general tenant-wide template for this participant type
        var tenantTemplate = await _dbSet
            .Where(t => t.TenantID == tenantId && (t.ExhibitionID == null || t.ExhibitionID == 0) && t.TargetParticipantType == participantType)
            .OrderByDescending(t => t.IsDefault)
            .FirstOrDefaultAsync();

        if (tenantTemplate != null)
            return tenantTemplate;

        // 3. Fallback to any default template in the tenant
        return await _dbSet
            .Where(t => t.TenantID == tenantId && t.IsDefault)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<BadgeTemplate>> GetTemplatesByExhibitionAsync(int tenantId, int exhibitionId)
    {
        return await _dbSet
            .Where(t => t.TenantID == tenantId && (t.ExhibitionID == exhibitionId || t.ExhibitionID == null))
            .OrderBy(t => t.TargetParticipantType)
            .ToListAsync();
    }
}
