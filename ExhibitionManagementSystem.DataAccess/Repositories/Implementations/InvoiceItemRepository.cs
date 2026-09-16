using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class InvoiceItemRepository : GenericRepository<InvoiceItem>, IInvoiceItemRepository
    {
        public InvoiceItemRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
