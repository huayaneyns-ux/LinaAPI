namespace ApiLinaAgbd.Models.Inventario.UnidadMedida
{
	public class UnidadMedidaSelectDto
	{
		public int Id { get; set; }

		public string Nombre { get; set; } = "";

		public string Abreviatura { get; set; } = "";

		public bool Estado { get; set; }
	}
}
