using ApiLinaAgbd.Models.Seguridad;

namespace ApiLinaAgbd.Repositories.Seguridad.Usuario
{
	public interface IUsuarioRepository
	{
		List<UsuarioSelectDto> Listar();
		UsuarioSelectDto? Obtener(int id);
	UsuarioSelectDto? ObtenerPorDocumento(string tipoDocumento, string numero);
	void ActualizarDatosIntegracion(int id, string nombreApellido, string? telefono, string correo, string origen);
	int Guardar(UsuarioInsertUpdateDto modelo);
		void Eliminar(int id);
	}
}
