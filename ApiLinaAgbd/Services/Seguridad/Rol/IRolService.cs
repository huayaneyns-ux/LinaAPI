using ApiLinaAgbd.Models.Seguridad;

namespace ApiLinaAgbd.Services.Seguridad.Rol
{
	public interface IRolService
	{
		List<RolSelectDto> Listar();
		RolSelectDto? Obtener(int id);
		int Insertar(RolInsertDto modelo);
		void Actualizar(RolUpdateDto modelo);
		void Eliminar(int id);
	}
}
