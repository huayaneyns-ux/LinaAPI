namespace ApiLinaAgbd.Models.Ventas.Caja
{
	public class CajaVentaInsertDto
	{
		public int IdCliente { get; set; }

		public int IdUsuario { get; set; }

		public decimal Igv { get; set; }

		public List<CajaDetalleInsertDto> Detalle { get; set; }

		public List<CajaPagoInsertDto> Pagos { get; set; }
	}
}
