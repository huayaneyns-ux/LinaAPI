namespace ApiLinaAgbd.Models.Inventario.Lote_Stock
{
	public class LoteFiltroDto
	{
		public string? codigoLote { get; set; }

		public int? idProducto { get; set; }

		public int? idProveedor { get; set; }

		public DateTime? fechaIngresoDesde { get; set; }

		public DateTime? fechaIngresoHasta { get; set; }

		public DateTime? fechaVencimientoDesde { get; set; }

		public DateTime? fechaVencimientoHasta { get; set; }
	}
}
