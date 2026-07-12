namespace ApiLinaAgbd.Models.Compras.Proveedor
{
	public class ProveedorUpdate
	{
		public int Id { get; set; }

		public string Ruc { get; set; } = string.Empty;

		public string RazonSocial { get; set; } = string.Empty;

		public string NombreContacto { get; set; } = string.Empty;

		public string Telefono { get; set; } = string.Empty;

		public int IdDireccion { get; set; }

		public bool Estado { get; set; }
	}
}
