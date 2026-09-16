using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

public interface IBadgeTemplateRepository : IGenericRepository<BadgeTemplate>
{
    Task<BadgeTemplate?> GetTemplateForParticipantAsync(int tenantId, int? exhibitionId, ParticipantType participantType);
    Task<IReadOnlyList<BadgeTemplate>> GetTemplatesByExhibitionAsync(int tenantId, int exhibitionId);
}
