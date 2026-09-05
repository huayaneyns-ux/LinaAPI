using ApiLinaAgbd.Models.Inventario.Productos;

namespace ApiLinaAgbd.Services.Inventario.Producto
{
	public interface IProductoService
	{
		List<ProductoSelectDto> Listar();
		void Insertar(ProductoInsertDto producto);
		void Actualizar(ProductoUpdateDto producto);
		void Eliminar(int id);
		ProductoSelectDto? ObtenerPorId(int id);
	}
}
