using ApiLinaAgbd.Models.Inventario.Lote;
using ApiLinaAgbd.Models.Inventario.Lote_Stock;
using ApiLinaAgbd.Repositories.Inventario.Lote;

namespace ApiLinaAgbd.Services.Inventario.Lote
{
	public class LoteService : ILoteService
	{
		private readonly ILoteRepository _loteRepository;

		public LoteService(ILoteRepository loteRepository)
		{
			_loteRepository = loteRepository;
		}

		public (int IdLote, string CodigoLote) Insertar(LoteInsertDto modelo)
		{
			return _loteRepository.Insertar(modelo);
		}

		public List<LoteSelectListarDto> Listar(LoteFiltroDto filtro)
		{
			return _loteRepository.Listar(filtro);
		}

		public LoteSelectDto? Obtener(int id)
		{
			return _loteRepository.Obtener(id);
		}

		public int InsertarMovimiento(MovimientoInsertDto modelo)
		{
			return _loteRepository.InsertarMovimiento(modelo);
		}

		public List<MovimientoSelectDto> ListarMovimientos(
			int? idProducto,
			int? tipo,
			DateTime? fechaDesde,
			DateTime? fechaHasta)
		{
			return _loteRepository.ListarMovimientos(idProducto, tipo, fechaDesde, fechaHasta);
		}
	}
}
