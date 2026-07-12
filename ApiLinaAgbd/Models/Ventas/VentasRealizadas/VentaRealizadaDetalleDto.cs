namespace ApiLinaAgbd.Models.Ventas.VentasRealizadas
{
	public class VentaRealizadaDetalleDto
	{
		public int Id { get; set; }

		public string Codigo { get; set; }

		public string Nombre { get; set; }

		public int Cantidad { get; set; }

		public decimal PrecioUnitario { get; set; }

		public decimal SubTotal { get; set; }
	}
}
