using ApiLinaAgbd.Models;
using ApiLinaAgbd.Models.Imagen;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace ApiLinaAgbd.Services
{
	public class CloudinaryService
	{
		private readonly Cloudinary _cloudinary;

		public CloudinaryService(IConfiguration configuration)
		{
			Account account = new Account(
				configuration["CloudinarySettings:CloudName"],
				configuration["CloudinarySettings:ApiKey"],
				configuration["CloudinarySettings:ApiSecret"]
			);

			_cloudinary = new Cloudinary(account);
		}

		//=========================================
		// SUBIR IMAGEN
		//=========================================
		public async Task<ImagenResponse> SubirImagen(IFormFile imagen)
		{
			using var stream = imagen.OpenReadStream();

			var uploadParams = new ImageUploadParams
			{
				File = new FileDescription(imagen.FileName, stream),
				Folder = "libreria"
			};

			var resultado = await _cloudinary.UploadAsync(uploadParams);

			return new ImagenResponse
			{
				RutaImagen = resultado.SecureUrl.ToString(),
				PublicId = resultado.PublicId
			};
		}

		//=========================================
		// ELIMINAR IMAGEN
		//=========================================
		public async Task<bool> EliminarImagen(string publicId)
		{

			var deleteParams = new DeletionParams(publicId);


			var resultado = await _cloudinary.DestroyAsync(deleteParams);


			return resultado.Result == "ok";

		}
	}
}