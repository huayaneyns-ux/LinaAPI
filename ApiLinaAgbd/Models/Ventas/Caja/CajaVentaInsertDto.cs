namespace ApiLinaAgbd.Models.Ventas.Caja
{
	public class CajaVentaInsertDto
	{
		public int? IdCliente { get; set; }

		public string TipoComprobante { get; set; } = "BOLETA";

		public CajaComprobanteFiscalDto? ClienteFiscal { get; set; }

		public int IdUsuario { get; set; }

		public decimal Igv { get; set; }

		public List<CajaDetalleInsertDto> Detalle { get; set; } = new();

		public List<CajaPagoInsertDto> Pagos { get; set; } = new();
	}
}
