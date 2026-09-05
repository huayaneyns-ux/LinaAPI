using ApiLinaAgbd.Models.Ventas;

namespace ApiLinaAgbd.Repositories.Ventas.Lugares
{
	public interface ILugaresRepository
	{
		List<DepartamentoDto> ListarDepartamentos();
		List<ProvinciaDto> ListarProvincias(int idDepartamento);
		List<DistritoDto> ListarDistritos(int idProvincia);
		List<DireccionDto> ListarDireccionesUsuario(int idUsuario);
		void InsertarDireccion(DireccionInsertDto modelo);
		void CambiarPrincipal(DireccionPrincipalDto modelo);
		void EliminarDireccion(int idUsuario, int idDireccion);
	}
}
