namespace ApiLinaAgbd.Models.Inventario.Lote
{
	public class LoteSelectDto
	{
		public int id_lote { get; set; }

		public string codigo_lote { get; set; } = string.Empty;

		public int id_producto { get; set; }

		public string codigo_producto { get; set; } = string.Empty;

		public string producto { get; set; } = string.Empty;

		public int id_proveedor { get; set; }

		public string proveedor { get; set; } = string.Empty;

		public DateTime fecha_ingreso { get; set; }

		public DateTime? fecha_fabricacion { get; set; }

		public DateTime? fecha_vencimiento { get; set; }

		public int cantidad_ingresada { get; set; }

		public decimal costo_unitario { get; set; }

		public decimal valorCompra { get; set; }

		public int? stockActual { get; set; }

		public int? diasParaVencer { get; set; }

		public string estadoLote { get; set; } = string.Empty;

		public List<LoteMovimientoDto> movimientos { get; set; } = new();
	}

	public class LoteMovimientoDto
	{
		public int id { get; set; }

		public DateTime fecha { get; set; }

		public string tipoMovimiento { get; set; } = string.Empty;

		public int cantidad { get; set; }

		public string? motivo { get; set; }
	}
}