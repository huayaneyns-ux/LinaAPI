namespace ApiLinaAgbd.Models.Ventas.Caja
{
	public class CajaClienteDto
	{
		public int Id { get; set; }

		public string NombreApellido { get; set; } = string.Empty;

		public string DNI { get; set; } = string.Empty;

		public string TipoDocumento { get; set; } = "DNI";

		public string Documento { get; set; } = string.Empty;

		public string Direccion { get; set; } = string.Empty;

		public string Telefono { get; set; } = string.Empty;

		public string Correo { get; set; } = string.Empty;
	}
}
