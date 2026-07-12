namespace ApiLinaAgbd.Models.Ventas.VentasRealizadas
{
	public class VentaRealizadaSelectDto
	{
		public int Id { get; set; }

		public string Cliente { get; set; }

		public string Vendedor { get; set; }

		public DateTime Fecha { get; set; }

		public int CantidadProductos { get; set; }

		public decimal Total { get; set; }

		public string Estado { get; set; }

		public decimal Igv { get; set; }
	}
}
