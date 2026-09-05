namespace ApiLinaAgbd.Services.Facturacion.Shared
{
	internal static class MontoEnLetras
	{
		private static readonly string[] Unidades =
		{
			"cero", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve",
			"diez", "once", "doce", "trece", "catorce", "quince", "dieciséis", "diecisiete",
			"dieciocho", "diecinueve", "veinte"
		};

		private static readonly string[] Decenas =
		{
			"", "", "veinti", "treinta", "cuarenta", "cincuenta",
			"sesenta", "setenta", "ochenta", "noventa"
		};

		private static readonly string[] Centenas =
		{
			"", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos",
			"seiscientos", "setecientos", "ochocientos", "novecientos"
		};

		public static string EnSoles(decimal importe)
		{
			var parteEntera = (long)Math.Truncate(importe);
			var centavos = (int)Math.Round((importe - parteEntera) * 100m, 0, MidpointRounding.AwayFromZero);

			if (centavos == 100)
			{
				parteEntera += 1;
				centavos = 0;
			}

			var letras = ConvertirEntero(parteEntera).ToUpperInvariant();
			return $"{letras} CON {centavos:00}/100 SOLES";
		}

		private static string ConvertirEntero(long numero)
		{
			if (numero == 0)
			{
				return "cero";
			}

			if (numero == 1)
			{
				return "un";
			}

			if (numero < 0)
			{
				return "menos " + ConvertirEntero(Math.Abs(numero));
			}

			var partes = new List<string>();

			var millones = numero / 1_000_000;
			var miles = (numero % 1_000_000) / 1000;
			var resto = numero % 1000;

			if (millones > 0)
			{
				partes.Add(millones == 1 ? "un millón" : ConvertirGrupo((int)millones) + " millones");
			}

			if (miles > 0)
			{
				partes.Add(miles == 1 ? "mil" : ConvertirGrupo((int)miles) + " mil");
			}

			if (resto > 0)
			{
				partes.Add(ConvertirGrupo((int)resto));
			}

			return string.Join(" ", partes);
		}

		private static string ConvertirGrupo(int numero)
		{
			if (numero == 100)
			{
				return "cien";
			}

			if (numero <= 20)
			{
				return Unidades[numero] == "uno" ? "un" : Unidades[numero];
			}

			var centena = numero / 100;
			var resto = numero % 100;
			var texto = centena > 0 ? Centenas[centena] : string.Empty;

			if (resto == 0)
			{
				return texto;
			}

			if (!string.IsNullOrEmpty(texto))
			{
				texto += " ";
			}

			if (resto <= 20)
			{
				return texto + (Unidades[resto] == "uno" ? "un" : Unidades[resto]);
			}

			var decena = resto / 10;
			var unidad = resto % 10;

			if (decena == 2)
			{
				return unidad == 0
					? texto + "veinte"
					: texto + "veinti" + (unidad == 1 ? "ún" : Unidades[unidad]);
			}

			texto += Decenas[decena];

			if (unidad > 0)
			{
				texto += " y " + (unidad == 1 ? "un" : Unidades[unidad]);
			}

			return texto;
		}
	}
}
