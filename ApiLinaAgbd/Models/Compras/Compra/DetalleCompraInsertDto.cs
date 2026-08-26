namespace ApiLinaAgbd.Models.Compras.Compra
{
	public class DetalleCompraInsertDto
	{
		public int id_compra { get; set; }

		public int id_producto { get; set; }

		public int cantidad { get; set; }

		public decimal costo_total { get; set; }
	}
}
