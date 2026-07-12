namespace ApiLinaAgbd.Models.Ventas.Caja
{
	public class CajaPagoInsertDto
	{
		public int IdVenta { get; set; }

		public int IdMetodoPago { get; set; }

		public decimal Monto { get; set; }

		public DateTime Fecha { get; set; }

		public string? CodigoOperacion { get; set; }
	}
}
