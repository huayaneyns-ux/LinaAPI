namespace ApiLinaAgbd.Models.Inventario.Lote
{
	public class LoteInsertDto
	{
		public int id_producto { get; set; }

		public int id_detalle_compra { get; set; }

		public DateTime fecha_ingreso { get; set; }

		public DateTime? fecha_fabricacion { get; set; }

		public DateTime? fecha_vencimiento { get; set; }

		public decimal costo_unitario { get; set; }

		public int cantidad { get; set; }
	}
}
