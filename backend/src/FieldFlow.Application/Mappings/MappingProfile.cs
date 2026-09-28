using AutoMapper;

namespace FieldFlow.Application.Mappings;

/// <summary>Mapeamentos entidade → DTO. Cada fase adiciona os <c>CreateMap</c> dos seus casos de uso.</summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
    }
}
