namespace ApiLinaAgbd.Models.Ventas
{
	//=========================================
	// DEPARTAMENTO
	//=========================================
	public class DepartamentoDto
	{
		public int id { get; set; }

		public string nombre { get; set; } = string.Empty;
	}


	//=========================================
	// PROVINCIA
	//=========================================
	public class ProvinciaDto
	{
		public int id { get; set; }

		public string nombre { get; set; } = string.Empty;

		public int idDepartamento { get; set; }
	}


	//=========================================
	// DISTRITO
	//=========================================
	public class DistritoDto
	{
		public int id { get; set; }

		public string nombre { get; set; } = string.Empty;

		public int idProvincia { get; set; }
	}
	//=========================================
	// DIRECCION LISTAR
	//=========================================
	public class DireccionDto
	{
		public int id { get; set; }

		public string nombreDireccion { get; set; } = string.Empty;

		public string referencia { get; set; } = string.Empty;


		public int idDistrito { get; set; }

		public string distrito { get; set; } = string.Empty;


		public int idProvincia { get; set; }

		public string provincia { get; set; } = string.Empty;


		public int idDepartamento { get; set; }

		public string departamento { get; set; } = string.Empty;


		public bool esPrincipal { get; set; }
	}



	//=========================================
	// INSERTAR DIRECCION
	//=========================================
	public class DireccionInsertDto
	{
		public int idUsuario { get; set; }

		public string nombreDireccion { get; set; } = string.Empty;

		public string referencia { get; set; } = string.Empty;

		public int idDistrito { get; set; }

		public bool esPrincipal { get; set; }
	}



	//=========================================
	// CAMBIAR DIRECCION PRINCIPAL
	//=========================================
	public class DireccionPrincipalDto
	{
		public int idUsuario { get; set; }

		public int idDireccion { get; set; }
	}

}