namespace ApiLinaAgbd.Models.Ventas.PedidosRecibidos
{
	public class PedidoSelectDto
	{
		public int id_pedido { get; set; }

		public DateTime fecha_pedido { get; set; }

		public DateTime? fecha_entrega { get; set; }

		public string tipo_entrega { get; set; } = string.Empty;

		public decimal igv { get; set; }

		public string? ruta_comprobante { get; set; }


		// Estado del pedido
		public int estadoPedido { get; set; }

		public string estadoPedidoNombre { get; set; } = string.Empty;


		// Cliente
		public int id_cliente { get; set; }

		public string cliente { get; set; } = string.Empty;

		public string telefono { get; set; } = string.Empty;


		// Pago
		public int? id_pago { get; set; }

		public decimal? monto { get; set; }

		public string? codigo_operacion { get; set; }


		// Método de pago
		public int? id_metodo_pago { get; set; }

		public string? metodoPago { get; set; }
	}
}