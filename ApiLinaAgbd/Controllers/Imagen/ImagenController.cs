using ApiLinaAgbd.Services.Imagen;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Imagen
{
	[ApiController]
	[Route("api/[controller]")]
	public class ImagenController : ControllerBase
	{

		private readonly ICloudinaryService _cloudinaryService;


		public ImagenController(
			ICloudinaryService cloudinaryService)
		{
			_cloudinaryService = cloudinaryService;
		}



		//=========================================
		// SUBIR IMAGEN
		//=========================================
		[HttpPost("Subir")]
		public async Task<IActionResult> Subir(IFormFile imagen)
		{

			if (imagen == null || imagen.Length == 0)
			{
				return BadRequest("No se recibió imagen");
			}


			var resultado = await _cloudinaryService.SubirImagen(imagen);


			return Ok(new
			{
				rutaImagen = resultado.RutaImagen,
				publicId = resultado.PublicId
			});

		}



		//=========================================
		// ELIMINAR IMAGEN
		//=========================================
		[HttpDelete("Eliminar")]
		public async Task<IActionResult> Eliminar(string publicId)
		{

			if (string.IsNullOrEmpty(publicId))
			{
				return BadRequest("No se recibió el PublicId");
			}



			bool eliminado = await _cloudinaryService.EliminarImagen(publicId);



			if (!eliminado)
			{
				return BadRequest("No se pudo eliminar la imagen");
			}


			return Ok("Imagen eliminada correctamente");

		}

	}
}
