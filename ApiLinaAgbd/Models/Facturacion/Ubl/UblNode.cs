using System.Text.Json.Serialization;

namespace ApiLinaAgbd.Models.Facturacion.Ubl
{
	public class UblNode
	{
		[JsonPropertyName("_attributes")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public Dictionary<string, string>? Attributes { get; set; }

		[JsonPropertyName("_text")]
		public object? Text { get; set; }

		public static UblNode Value(object text)
		{
			return new UblNode { Text = text };
		}

		public static UblNode Attr(object text, string attributeName, string attributeValue)
		{
			return new UblNode
			{
				Text = text,
				Attributes = new Dictionary<string, string>
				{
					[attributeName] = attributeValue
				}
			};
		}

		public static UblNode Amount(decimal amount, string currency)
		{
			// SUNAT recibe los importes monetarios con máximo dos decimales.
			// Redondear aquí evita serializar valores como 5.846666666666667
			// producidos por divisiones o por datos históricos con mayor escala.
			return Attr(Math.Round(amount, 2, MidpointRounding.AwayFromZero), "currencyID", currency);
		}
	}
}
