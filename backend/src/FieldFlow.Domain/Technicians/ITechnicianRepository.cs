using FieldFlow.Domain.Interfaces;

namespace FieldFlow.Domain.Technicians;

public interface ITechnicianRepository : IRepository<Technician>
{
    Task<Technician?> GetWithSkillsAsync(Guid technicianId, CancellationToken cancellationToken = default);
}
