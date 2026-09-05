using ApiLinaAgbd.Models.Inventario.Lote;
using ApiLinaAgbd.Models.Inventario.Lote_Stock;

namespace ApiLinaAgbd.Repositories.Inventario.Lote
{
	public interface ILoteRepository
	{
		(int IdLote, string CodigoLote) Insertar(LoteInsertDto modelo);
		List<LoteSelectListarDto> Listar(LoteFiltroDto filtro);
		LoteSelectDto? Obtener(int id);
		int InsertarMovimiento(MovimientoInsertDto modelo);
		List<MovimientoSelectDto> ListarMovimientos(
			int? idProducto,
			int? tipo,
			DateTime? fechaDesde,
			DateTime? fechaHasta);
	}
}
