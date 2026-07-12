using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Inventario.Productos;

namespace ApiLinaAgbd.Controllers.Inventario
{
	[ApiController]
	[Route("api/[controller]")]
	public class ProductoController : ControllerBase
	{
		private readonly Conexion _conexion;

		public ProductoController(Conexion conexion)
		{
			_conexion = conexion;
		}

		//=========================================
		// LISTAR
		//=========================================
		[HttpGet("Lista")]
		public IActionResult ObtenerProductos()
		{
			List<ProductoSelectDto> lista = new();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_SEL_PRODUCTO_LISTAR", con);
				cmd.CommandType = CommandType.StoredProcedure;

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new ProductoSelectDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Codigo = dr["codigo"].ToString(),
						Sku = dr["sku"].ToString(),
						Nombre = dr["nombre"].ToString(),
						Descripcion = dr["descripcion"] == DBNull.Value ? null : dr["descripcion"].ToString(),

						PrecioVenta = Convert.ToDecimal(dr["precio_venta"]),
						FactorConversion = dr["factor_conversion"] == DBNull.Value ? null : Convert.ToDecimal(dr["factor_conversion"]),
						StockMinimo = Convert.ToInt32(dr["stock_minimo"]),
						RutaImagen = dr["ruta_imagen"] == DBNull.Value ? null : dr["ruta_imagen"].ToString(),
						PublicIdImagen = dr["public_id_imagen"] == DBNull.Value? null: dr["public_id_imagen"].ToString(),
						Estado = Convert.ToBoolean(dr["estado"]),

						IdCategoria = Convert.ToInt32(dr["id_categoria"]),
						Categoria = dr["categoria"].ToString(),

						IdProveedor = Convert.ToInt32(dr["id_proveedor"]),
						Ruc = dr["ruc"].ToString(),
						RazonSocial = dr["razon_social"].ToString(),
						NombreContacto = dr["nombre_contacto"].ToString(),
						Telefono = dr["telefono"].ToString(),

						IdMarca = Convert.ToInt32(dr["id_marca"]),
						Marca = dr["marca"].ToString(),

						IdUnidadMedida = Convert.ToInt32(dr["id_unidad_medida"]),
						UnidadMedida = dr["unidad_medida"].ToString(),
						Abreviatura = dr["abreviatura"].ToString()
					});
				}
			}

			return Ok(lista);
		}

		//=========================================
		// INSERTAR
		//=========================================
		[HttpPost]
		public IActionResult InsertarProducto([FromBody] ProductoInsertDto producto)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_INS_PRODUCTO", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Codigo", producto.Codigo);
				cmd.Parameters.AddWithValue("@Sku", producto.Sku);
				cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
				cmd.Parameters.AddWithValue("@Descripcion", (object?)producto.Descripcion ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@PrecioVenta", producto.PrecioVenta);
				cmd.Parameters.AddWithValue("@FactorConversion", (object?)producto.FactorConversion ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@StockMinimo", producto.StockMinimo);
				cmd.Parameters.AddWithValue("@RutaImagen", (object?)producto.RutaImagen ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@PublicIdImagen",(object?)producto.PublicIdImagen ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@IdCategoria", producto.IdCategoria);
				cmd.Parameters.AddWithValue("@IdProveedor", producto.IdProveedor);
				cmd.Parameters.AddWithValue("@IdMarca", producto.IdMarca);
				cmd.Parameters.AddWithValue("@IdUnidadMedida", producto.IdUnidadMedida);

				cmd.ExecuteNonQuery();
			}

			return Ok("Producto registrado correctamente.");
		}

		//=========================================
		// ACTUALIZAR
		//=========================================
		[HttpPut]
		public IActionResult ActualizarProducto([FromBody] ProductoUpdateDto producto)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_UPD_PRODUCTO", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", producto.Id);
				cmd.Parameters.AddWithValue("@Codigo", producto.Codigo);
				cmd.Parameters.AddWithValue("@Sku", producto.Sku);
				cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
				cmd.Parameters.AddWithValue("@Descripcion", (object?)producto.Descripcion ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@PrecioVenta", producto.PrecioVenta);
				cmd.Parameters.AddWithValue("@FactorConversion", (object?)producto.FactorConversion ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@StockMinimo", producto.StockMinimo);
				cmd.Parameters.AddWithValue("@RutaImagen", (object?)producto.RutaImagen ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@PublicIdImagen", (object?)producto.PublicIdImagen ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@IdCategoria", producto.IdCategoria);
				cmd.Parameters.AddWithValue("@IdProveedor", producto.IdProveedor);
				cmd.Parameters.AddWithValue("@IdMarca", producto.IdMarca);
				cmd.Parameters.AddWithValue("@IdUnidadMedida", producto.IdUnidadMedida);

				cmd.ExecuteNonQuery();
			}

			return Ok("Producto actualizado correctamente.");
		}

		//=========================================
		// ELIMINAR
		//=========================================
		[HttpDelete("{id}")]
		public IActionResult EliminarProducto(int id)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_DEL_PRODUCTO", con);
				cmd.CommandType = CommandType.StoredProcedure;

				// 🔥 AQUÍ ESTÁ EL ERROR CORREGIDO
				cmd.Parameters.AddWithValue("@IdProducto", id);

				cmd.ExecuteNonQuery();
			}

			return Ok("Producto eliminado correctamente.");
		}

		//=========================================
		// OBTENER
		//=========================================

		[HttpGet("{id}")]
		public IActionResult ObtenerProductoPorId(int id)
		{
			ProductoSelectDto producto = null;

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_SEL_PRODUCTO_OBTENER", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", id);

				SqlDataReader dr = cmd.ExecuteReader();

				if (dr.Read())
				{
					producto = new ProductoSelectDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Codigo = dr["codigo"].ToString(),
						Sku = dr["sku"].ToString(),
						Nombre = dr["nombre"].ToString(),
						Descripcion = dr["descripcion"] == DBNull.Value ? null : dr["descripcion"].ToString(),

						PrecioVenta = Convert.ToDecimal(dr["precio_venta"]),
						FactorConversion = dr["factor_conversion"] == DBNull.Value ? null : Convert.ToDecimal(dr["factor_conversion"]),
						StockMinimo = Convert.ToInt32(dr["stock_minimo"]),
						RutaImagen = dr["ruta_imagen"] == DBNull.Value ? null : dr["ruta_imagen"].ToString(),
						PublicIdImagen = dr["public_id_imagen"] == DBNull.Value ? null : dr["public_id_imagen"].ToString(),
						Estado = Convert.ToBoolean(dr["estado"]),

						IdCategoria = Convert.ToInt32(dr["id_categoria"]),
						Categoria = dr["categoria"].ToString(),

						IdProveedor = Convert.ToInt32(dr["id_proveedor"]),
						Ruc = dr["ruc"].ToString(),
						RazonSocial = dr["razon_social"].ToString(),
						NombreContacto = dr["nombre_contacto"].ToString(),
						Telefono = dr["telefono"].ToString(),

						IdMarca = Convert.ToInt32(dr["id_marca"]),
						Marca = dr["marca"].ToString(),

						IdUnidadMedida = Convert.ToInt32(dr["id_unidad_medida"]),
						UnidadMedida = dr["unidad_medida"].ToString(),
						Abreviatura = dr["abreviatura"].ToString()
					};
				}
			}

			if (producto == null)
				return NotFound("Producto no encontrado.");

			return Ok(producto);
		}
	}
}