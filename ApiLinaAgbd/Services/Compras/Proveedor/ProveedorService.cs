using System.Text.Json;
using ApiLinaAgbd.Models.Compras.Proveedor;
using ApiLinaAgbd.Repositories.Compras.Proveedor;

namespace ApiLinaAgbd.Services.Compras.Proveedor
{
	public class ProveedorService : IProveedorService
	{
		private readonly IProveedorRepository _proveedorRepository;

		public ProveedorService(IProveedorRepository proveedorRepository)
		{
			_proveedorRepository = proveedorRepository;
		}

		public List<ProveedorSelectDto> Listar()
		{
			return _proveedorRepository.Listar();
		}

		public ProveedorSelectDto? Obtener(int id)
		{
			return _proveedorRepository.Obtener(id);
		}

		public void Insertar(JsonElement json)
		{
			_proveedorRepository.Insertar(json);
		}

		public void Actualizar(ProveedorUpdate proveedor)
		{
			_proveedorRepository.Actualizar(proveedor);
		}

		public void Eliminar(int id)
		{
			_proveedorRepository.Eliminar(id);
		}
	}
}
