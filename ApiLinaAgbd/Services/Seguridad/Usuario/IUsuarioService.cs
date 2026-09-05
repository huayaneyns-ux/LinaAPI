using ApiLinaAgbd.Models.Seguridad;

namespace ApiLinaAgbd.Services.Seguridad.Usuario
{
	public interface IUsuarioService
	{
		UsuarioLoginResponseDto? Login(UsuarioLoginDto modelo);
		List<UsuarioSelectDto> Listar();
		UsuarioSelectDto? Obtener(int id);
		int Guardar(UsuarioInsertUpdateDto modelo);
		void Eliminar(int id);
	}
}
