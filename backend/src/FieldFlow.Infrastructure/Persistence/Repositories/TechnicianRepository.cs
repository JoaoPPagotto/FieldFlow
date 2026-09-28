using FieldFlow.Domain.Technicians;
using Microsoft.EntityFrameworkCore;

namespace FieldFlow.Infrastructure.Persistence.Repositories;

internal sealed class TechnicianRepository : Repository<Technician>, ITechnicianRepository
{
    public TechnicianRepository(ApplicationDbContext context) : base(context) { }

    public Task<Technician?> GetWithSkillsAsync(Guid technicianId, CancellationToken cancellationToken = default)
        => DbSet.Include(t => t.Skills).FirstOrDefaultAsync(t => t.TechnicianId == technicianId, cancellationToken);
}
