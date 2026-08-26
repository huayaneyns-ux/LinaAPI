namespace ApiLinaAgbd.Models.Seguridad
{

	//=========================================
	// LISTAR / OBTENER ROL
	//=========================================
	public class RolSelectDto
	{
		public int id { get; set; }

		public string nombre { get; set; } = string.Empty;

		public bool estado { get; set; }
	}



	//=========================================
	// INSERTAR ROL
	//=========================================
	public class RolInsertDto
	{
		public string nombre { get; set; } = string.Empty;
	}



	//=========================================
	// ACTUALIZAR ROL
	//=========================================
	public class RolUpdateDto
	{
		public int id { get; set; }

		public string nombre { get; set; } = string.Empty;
	}

}