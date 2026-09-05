using ApiLinaAgbd.Models.Seguridad;

namespace ApiLinaAgbd.Repositories.Seguridad.Rol
{
	public interface IRolRepository
	{
		List<RolSelectDto> Listar();
		RolSelectDto? Obtener(int id);
		int Insertar(RolInsertDto modelo);
		void Actualizar(RolUpdateDto modelo);
		void Eliminar(int id);
	}
}
