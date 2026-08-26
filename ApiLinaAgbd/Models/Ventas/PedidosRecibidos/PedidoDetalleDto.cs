namespace ApiLinaAgbd.Models.Ventas.PedidosRecibidos
{
	public class PedidoDetalleDto
	{
		public int id_detalle_pedido { get; set; }

		public int id_producto { get; set; }

		public string producto { get; set; } = string.Empty;

		public string codigo { get; set; } = string.Empty;

		public string? ruta_imagen { get; set; }

		public int cantidad { get; set; }

		public decimal precio_venta { get; set; }
	}
}