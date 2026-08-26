namespace ApiLinaAgbd.Models.Ventas.PedidosRecibidos
{
	public class PedidoInsertDto
	{
		// PEDIDO
		public int idCliente { get; set; }
		public int idDireccion { get; set; }

		public DateTime fechaPedido { get; set; }

		public DateTime? fechaEntrega { get; set; }

		public string tipoEntrega { get; set; } = string.Empty;

		public decimal igv { get; set; }


		// PAGO
		public int idMetodoPago { get; set; }

		public decimal monto { get; set; }

		public string? codigoOperacion { get; set; }

		public string? rutaComprobante { get; set; }


		// DETALLE
		public List<PedidoDetalleInsertDto> detalle { get; set; } = new();
	}


	public class PedidoDetalleInsertDto
	{
		public int idProducto { get; set; }

		public int cantidad { get; set; }
	}
}