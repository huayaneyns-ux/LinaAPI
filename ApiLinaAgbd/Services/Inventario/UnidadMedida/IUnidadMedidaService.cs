using ApiLinaAgbd.Models.Inventario.UnidadMedida;

namespace ApiLinaAgbd.Services.Inventario.UnidadMedida
{
	public interface IUnidadMedidaService
	{
		List<UnidadMedidaSelectDto> Listar();
		void Insertar(UnidadMedidaInsertDto unidad);
		void Actualizar(UnidadMedidaUpdateDto unidad);
		void Eliminar(int id);
		UnidadMedidaObtenerIdDto? ObtenerPorId(int id);
	}
}
