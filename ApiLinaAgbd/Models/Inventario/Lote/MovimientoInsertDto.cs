namespace ApiLinaAgbd.Models.Inventario.Lote
{
	public class MovimientoInsertDto
	{
		public int id_usuario { get; set; }

		public int id_lote { get; set; }

		public int id_producto { get; set; }

		public int tipo { get; set; }

		public int cantidad { get; set; }

		public string? motivo { get; set; }
	}
}