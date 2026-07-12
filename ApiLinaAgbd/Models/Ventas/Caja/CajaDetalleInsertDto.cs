namespace ApiLinaAgbd.Models.Ventas.Caja
{
	public class CajaDetalleInsertDto
	{
		public int IdProducto { get; set; }

		public int Cantidad { get; set; }

		public decimal PrecioUnitario { get; set; }
	}
}
