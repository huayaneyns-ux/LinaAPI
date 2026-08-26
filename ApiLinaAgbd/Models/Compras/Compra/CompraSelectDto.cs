namespace ApiLinaAgbd.Models.Compras.Compra
{
	public class CompraSelectDto
	{
		public int id_compra { get; set; }

		public int id_usuario { get; set; }
		public string usuario { get; set; } = "";

		public int id_proveedor { get; set; }
		public string proveedor { get; set; } = "";

		public DateTime fecha_compra { get; set; }
		public DateTime? fecha_recepcion { get; set; }

		public decimal total_compra { get; set; }

		public string estado { get; set; } = "";

		public List<CompraDetalleSelectDto> detalles { get; set; } = new();
	}


	public class CompraDetalleSelectDto
	{
		public int id_detalle_compra { get; set; }

		public int id_producto { get; set; }

		public string codigo_producto { get; set; } = "";

		public string producto { get; set; } = "";

		public int cantidad { get; set; }

		public decimal costo_total { get; set; }

		public decimal costo_unitario { get; set; }

		public int? id_lote { get; set; }

		public string? codigo_lote { get; set; }

		public DateTime? fecha_vencimiento { get; set; }

		public int? stock_actual { get; set; }
	}
}
