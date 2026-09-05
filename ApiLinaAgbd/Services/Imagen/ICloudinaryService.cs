using ApiLinaAgbd.Models.Imagen;

namespace ApiLinaAgbd.Services.Imagen
{
	public interface ICloudinaryService
	{
		Task<ImagenResponse> SubirImagen(IFormFile imagen);
		Task<bool> EliminarImagen(string publicId);
	}
}
