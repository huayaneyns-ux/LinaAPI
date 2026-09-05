namespace ApiLinaAgbd.Models.Compras.Proveedor
{
	public class ProveedorSelectDto
	{
		public int Id { get; set; }

		public string? Ruc { get; set; }

		public string? RazonSocial { get; set; }

		public string? NombreContacto { get; set; }

		public string? Telefono { get; set; }

		public bool Estado { get; set; }

		public int IdDireccion { get; set; }

		public string? Direccion { get; set; }

		public string? Distrito { get; set; }

		public string? Provincia { get; set; }

		public string? Departamento { get; set; }
	}
}
