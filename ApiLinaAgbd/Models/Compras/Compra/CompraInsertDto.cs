namespace ApiLinaAgbd.Models.Compras.Compra
{
	public class CompraInsertDto
	{
		public int id_usuario { get; set; }

		public int id_proveedor { get; set; }

		public DateTime fecha_compra { get; set; }

		public DateTime? fecha_recepcion { get; set; }
	}
}
