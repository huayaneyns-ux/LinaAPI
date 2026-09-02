namespace ApiLinaAgbd.Models.Seguridad
{

	//=========================================
	// LISTAR / OBTENER USUARIO
	//=========================================
	public class UsuarioSelectDto
	{

		public int id { get; set; }


		public string nombreApellido { get; set; } = string.Empty;


		public string dni { get; set; } = string.Empty;


		public string sexo { get; set; } = string.Empty;


		public string? telefono { get; set; }


		public string correo { get; set; } = string.Empty;


		public int idRol { get; set; }


		public string rol { get; set; } = string.Empty;


		public bool estado { get; set; }

	}





	//=========================================
	// INSERTAR / ACTUALIZAR USUARIO
	//=========================================
	public class UsuarioInsertUpdateDto
	{

		// Si viene null = INSERTAR
		// Si viene con valor = ACTUALIZAR
		public int? idUsuario { get; set; }



		public string nombreApellido { get; set; } = string.Empty;



		public string dni { get; set; } = string.Empty;



		public string sexo { get; set; } = string.Empty;



		public string? telefono { get; set; }



		public string correo { get; set; } = string.Empty;



		public string contrasena { get; set; } = string.Empty;



		public int idRol { get; set; }



		public bool estado { get; set; } = true;

	}





	//=========================================
	// LOGIN USUARIO (futuro)
	//=========================================
	public class UsuarioLoginDto
	{

		public string usuario { get; set; } = string.Empty;


		public string contrasena { get; set; } = string.Empty;

	}

	public class UsuarioLoginResponseDto
	{
		public string id { get; set; } = string.Empty;

		public string username { get; set; } = string.Empty;

		public string nombres { get; set; } = string.Empty;

		public string apellidos { get; set; } = string.Empty;

		public string rol { get; set; } = string.Empty;

		public string email { get; set; } = string.Empty;

		public string estado { get; set; } = string.Empty;

		public string sucursal { get; set; } = string.Empty;

		public string? telefono { get; set; }

		public string createdAt { get; set; } = string.Empty;

		public string updatedAt { get; set; } = string.Empty;
	}





	//=========================================
	// CAMBIO DE ESTADO
	// (opcional)
	//=========================================
	public class UsuarioEstadoDto
	{

		public int idUsuario { get; set; }


		public bool estado { get; set; }

	}

}
