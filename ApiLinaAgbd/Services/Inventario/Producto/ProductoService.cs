using ApiLinaAgbd.Models.Inventario.Productos;
using ApiLinaAgbd.Repositories.Inventario.Producto;

namespace ApiLinaAgbd.Services.Inventario.Producto
{
	public class ProductoService : IProductoService
	{
		private readonly IProductoRepository _productoRepository;

		public ProductoService(IProductoRepository productoRepository)
		{
			_productoRepository = productoRepository;
		}

		public List<ProductoSelectDto> Listar()
		{
			return _productoRepository.Listar();
		}

		public void Insertar(ProductoInsertDto producto)
		{
			_productoRepository.Insertar(producto);
		}

		public void Actualizar(ProductoUpdateDto producto)
		{
			_productoRepository.Actualizar(producto);
		}

		public void Eliminar(int id)
		{
			_productoRepository.Eliminar(id);
		}

		public ProductoSelectDto? ObtenerPorId(int id)
		{
			return _productoRepository.ObtenerPorId(id);
		}
	}
}
