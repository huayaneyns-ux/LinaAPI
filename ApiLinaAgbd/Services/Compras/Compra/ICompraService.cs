using ApiLinaAgbd.Models.Compras.Compra;

namespace ApiLinaAgbd.Services.Compras.Compra
{
	public record CompraRegistrarResult(bool Success, string Mensaje, int IdCompra = 0);

	public interface ICompraService
	{
		CompraRegistrarResult RegistrarCompleta(CompraCompletaInsertDto modelo);
		List<CompraDetalleSelectDto> ObtenerDetalle(int id);
		List<CompraListaDto> Listar();
	}
}
