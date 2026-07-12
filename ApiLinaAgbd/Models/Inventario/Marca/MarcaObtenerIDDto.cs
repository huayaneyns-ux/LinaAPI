namespace ApiLinaAgbd.Models.Inventario.Marca
{
	public class MarcaObtenerIDDto
	{
		public int Id { get; set; }
		public string Nombre { get; set; } = string.Empty;
		public bool Estado { get; set; }
		public string? UrlImagen { get; set; }
	}
}
