namespace ApiLinaAgbd.Models.Ventas.VentasRealizadas
{
	public class VentaRealizadaPagoDto
	{
		public int Id { get; set; }

		public string MetodoPago { get; set; }

		public decimal Monto { get; set; }

		public DateTime Fecha { get; set; }

		public string CodigoOperacion { get; set; }
	}
}
