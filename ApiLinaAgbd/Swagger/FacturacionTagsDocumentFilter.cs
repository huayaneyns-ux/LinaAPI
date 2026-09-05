using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiLinaAgbd.Swagger
{
	/// <summary>
	/// Descripciones bajo el título de cada etiqueta Swagger de Facturación.
	/// </summary>
	public sealed class FacturacionTagsDocumentFilter : IDocumentFilter
	{
		public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
		{
			swaggerDoc.Tags ??= new HashSet<OpenApiTag>();

			Upsert(swaggerDoc, "Facturacion - Factura / Boleta (01 / 03)",
				"Factura · Código: 01 | Boleta de Venta · Código: 03");

			Upsert(swaggerDoc, "Facturacion - Liquidacion Compra (04)",
				"Liquidación de Compra · Código: 04");

			Upsert(swaggerDoc, "Facturacion - Nota Credito (NC)",
				"NC · Código: 07");

			Upsert(swaggerDoc, "Facturacion - Nota Debito (ND)",
				"ND · Código: 08");

			Upsert(swaggerDoc, "Facturacion - Documentos",
				"Consulta, sincronización SUNAT, PDF y anulación de todos los tipos de documento");
		}

		private static void Upsert(OpenApiDocument doc, string name, string description)
		{
			var existing = doc.Tags!.FirstOrDefault(t => t.Name == name);
			if (existing is null)
			{
				doc.Tags.Add(new OpenApiTag { Name = name, Description = description });
				return;
			}

			existing.Description = description;
		}
	}
}
