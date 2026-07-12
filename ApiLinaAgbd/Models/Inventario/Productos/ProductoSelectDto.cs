namespace ApiLinaAgbd.Models.Inventario.Productos
{
	public class ProductoSelectDto
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
		public bool Estado { get; set; }

		// Categoría
		public int IdCategoria { get; set; }
		public string Categoria { get; set; }

		// Proveedor
		public int IdProveedor { get; set; }
		public string Ruc { get; set; }
		public string RazonSocial { get; set; }
		public string NombreContacto { get; set; }
		public string Telefono { get; set; }

		// Marca
		public int IdMarca { get; set; }
		public string Marca { get; set; }

		// Unidad de medida
		public int IdUnidadMedida { get; set; }
		public string UnidadMedida { get; set; }
		public string Abreviatura { get; set; }
	}
}