using ApiLinaAgbd.Models.Inventario.UnidadMedida;
using ApiLinaAgbd.Repositories.Inventario.UnidadMedida;

namespace ApiLinaAgbd.Services.Inventario.UnidadMedida
{
	public class UnidadMedidaService : IUnidadMedidaService
	{
		private readonly IUnidadMedidaRepository _unidadMedidaRepository;

		public UnidadMedidaService(IUnidadMedidaRepository unidadMedidaRepository)
		{
			_unidadMedidaRepository = unidadMedidaRepository;
		}

		public List<UnidadMedidaSelectDto> Listar()
		{
			return _unidadMedidaRepository.Listar();
		}

		public void Insertar(UnidadMedidaInsertDto unidad)
		{
			_unidadMedidaRepository.Insertar(unidad);
		}

		public void Actualizar(UnidadMedidaUpdateDto unidad)
		{
			_unidadMedidaRepository.Actualizar(unidad);
		}

		public void Eliminar(int id)
		{
			_unidadMedidaRepository.Eliminar(id);
		}

		public UnidadMedidaObtenerIdDto? ObtenerPorId(int id)
		{
			return _unidadMedidaRepository.ObtenerPorId(id);
		}
	}
}
