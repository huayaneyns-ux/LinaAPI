using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.MetodoPago;

namespace ApiLinaAgbd.Repositories.MetodoPago
{
	public class MetodoPagoRepository : IMetodoPagoRepository
	{
		private readonly Conexion _conexion;

		public MetodoPagoRepository(Conexion conexion)
		{
			_conexion = conexion;
		}

		public List<MetodoPagoSelectDto> Listar()
		{
			var lista = new List<MetodoPagoSelectDto>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_PRO_SEL_METODO_PAGO_LISTAR",
					con
				);

				cmd.CommandType = CommandType.StoredProcedure;

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new MetodoPagoSelectDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Nombre = dr["nombre"].ToString(),
						Estado = Convert.ToBoolean(dr["estado"])
					});
				}
			}

			return lista;
		}
	}
}
