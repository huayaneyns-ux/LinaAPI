using ApiLinaAgbd.Models.Seguridad;

namespace ApiLinaAgbd.Repositories.Seguridad.Auditoria;

public interface IAuditoriaRepository
{
	AuditoriaPageDto Listar(int page, int pageSize, string? search, string? sortBy, string? sortDirection);
}
