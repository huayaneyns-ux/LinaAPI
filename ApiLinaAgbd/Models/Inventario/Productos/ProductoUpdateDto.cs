namespace ApiLinaAgbd.Models.Inventario.Productos
{
	public class ProductoUpdateDto
	{
		public int Id { get; set; }

		public string Codigo { get; set; }
		public string Sku { get; set; }
		public string Nombre { get; set; }
		public string? Descripcion { get; set; }
		public decimal PrecioVenta { get; set; }
		public decimal? FactorConversion { get; set; }
		public int StockMinimo { get; set; }
		public string? RutaImagen { get; set; }
		public string? PublicIdImagen { get; set; }
		public int IdCategoria { get; set; }
		public int IdProveedor { get; set; }
		public int IdMarca { get; set; }
		public int IdUnidadMedida { get; set; }
	}
}
