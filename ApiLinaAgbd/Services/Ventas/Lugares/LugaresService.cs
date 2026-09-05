using ApiLinaAgbd.Models.Ventas;
using ApiLinaAgbd.Repositories.Ventas.Lugares;

namespace ApiLinaAgbd.Services.Ventas.Lugares
{
	public class LugaresService : ILugaresService
	{
		private readonly ILugaresRepository _lugaresRepository;

		public LugaresService(ILugaresRepository lugaresRepository)
		{
			_lugaresRepository = lugaresRepository;
		}

		public List<DepartamentoDto> ListarDepartamentos()
		{
			return _lugaresRepository.ListarDepartamentos();
		}

		public List<ProvinciaDto> ListarProvincias(int idDepartamento)
		{
			return _lugaresRepository.ListarProvincias(idDepartamento);
		}

		public List<DistritoDto> ListarDistritos(int idProvincia)
		{
			return _lugaresRepository.ListarDistritos(idProvincia);
		}

		public List<DireccionDto> ListarDireccionesUsuario(int idUsuario)
		{
			return _lugaresRepository.ListarDireccionesUsuario(idUsuario);
		}

		public void InsertarDireccion(DireccionInsertDto modelo)
		{
			_lugaresRepository.InsertarDireccion(modelo);
		}

		public void CambiarPrincipal(DireccionPrincipalDto modelo)
		{
			_lugaresRepository.CambiarPrincipal(modelo);
		}

		public void EliminarDireccion(int idUsuario, int idDireccion)
		{
			_lugaresRepository.EliminarDireccion(idUsuario, idDireccion);
		}
	}
}
