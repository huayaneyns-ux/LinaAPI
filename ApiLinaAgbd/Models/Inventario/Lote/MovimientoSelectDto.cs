namespace ApiLinaAgbd.Models.Inventario.Lote
{
	public class MovimientoSelectDto
	{
		public int idMovimiento { get; set; }

		public DateTime fecha { get; set; }

		public int idTipoMovimiento { get; set; }

		public string tipoMovimiento { get; set; } = "";


		public int idProducto { get; set; }

		public string codigoProducto { get; set; } = "";

		public string producto { get; set; } = "";


		public int idLote { get; set; }

		public string codigoLote { get; set; } = "";


		public int idUsuario { get; set; }

		public string usuario { get; set; } = "";


		public int cantidad { get; set; }

		public string? motivo { get; set; }


		public int stockActual { get; set; }
	}
}
