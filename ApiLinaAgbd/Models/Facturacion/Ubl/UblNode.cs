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
			return Attr(amount, "currencyID", currency);
		}
	}
}
