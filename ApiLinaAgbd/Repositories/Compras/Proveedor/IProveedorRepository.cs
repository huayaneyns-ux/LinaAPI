using System.Text.Json;
using ApiLinaAgbd.Models.Compras.Proveedor;

namespace ApiLinaAgbd.Repositories.Compras.Proveedor
{
	public interface IProveedorRepository
	{
		List<ProveedorSelectDto> Listar();
		ProveedorSelectDto? Obtener(int id);
		void Insertar(JsonElement json);
		void Actualizar(ProveedorUpdate proveedor);
		void Eliminar(int id);
	}
}
