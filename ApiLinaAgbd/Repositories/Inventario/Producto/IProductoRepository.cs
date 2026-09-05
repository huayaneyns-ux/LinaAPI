using ApiLinaAgbd.Models.Inventario.Productos;

namespace ApiLinaAgbd.Repositories.Inventario.Producto
{
	public interface IProductoRepository
	{
		List<ProductoSelectDto> Listar();
		void Insertar(ProductoInsertDto producto);
		void Actualizar(ProductoUpdateDto producto);
		void Eliminar(int id);
		ProductoSelectDto? ObtenerPorId(int id);
	}
}
