namespace ApiLinaAgbd.Models.Compras.Compra
{
	public class CompraCompletaInsertDto
	{
		public int id_usuario { get; set; }

		public int id_proveedor { get; set; }

		public DateTime fecha_compra { get; set; }

		public DateTime? fecha_recepcion { get; set; }


		public List<DetalleCompraCompletaDto> detalles { get; set; } = new();
	}


	public class DetalleCompraCompletaDto
	{
		public int id_producto { get; set; }

		public int cantidad { get; set; }

		public decimal costo_total { get; set; }


		// Datos para generar el lote
		public DateTime? fecha_fabricacion { get; set; }

		public DateTime? fecha_vencimiento { get; set; }
	}
}
