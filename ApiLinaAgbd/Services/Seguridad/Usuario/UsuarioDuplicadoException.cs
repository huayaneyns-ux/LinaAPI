namespace ApiLinaAgbd.Services.Seguridad.Usuario;

public sealed class UsuarioDuplicadoException : Exception
{
	public string Campo { get; }

	public UsuarioDuplicadoException(string campo, string mensaje) : base(mensaje)
	{
		Campo = campo;
	}
}
