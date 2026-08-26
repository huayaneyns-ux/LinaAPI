namespace ApiLinaAgbd.Models.Compras.Compra
{
	public class CompraListaDto
	{
		public int id_compra { get; set; }

		public int id_usuario { get; set; }

		public string usuario { get; set; } = "";


		public int id_proveedor { get; set; }

		public string proveedor { get; set; } = "";


		public DateTime fecha_compra { get; set; }

		public DateTime? fecha_recepcion { get; set; }


		public decimal total_compra { get; set; }


		public bool estado { get; set; }
	}
}
