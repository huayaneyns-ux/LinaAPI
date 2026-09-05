using ApiLinaAgbd.Models.Inventario.UnidadMedida;

namespace ApiLinaAgbd.Repositories.Inventario.UnidadMedida
{
	public interface IUnidadMedidaRepository
	{
		List<UnidadMedidaSelectDto> Listar();
		void Insertar(UnidadMedidaInsertDto unidad);
		void Actualizar(UnidadMedidaUpdateDto unidad);
		void Eliminar(int id);
		UnidadMedidaObtenerIdDto? ObtenerPorId(int id);
	}
}
