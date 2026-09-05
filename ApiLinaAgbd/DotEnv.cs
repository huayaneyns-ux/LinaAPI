namespace ApiLinaAgbd
{
	/// <summary>
	/// Carga un archivo .env simple (KEY=VALUE) al entorno del proceso.
	/// </summary>
	internal static class DotEnv
	{
		public static void Load()
		{
			foreach (var candidate in CandidatePaths())
			{
				if (File.Exists(candidate))
				{
					LoadFile(candidate);
					return;
				}
			}
		}

		private static IEnumerable<string> CandidatePaths()
		{
			yield return Path.Combine(Directory.GetCurrentDirectory(), ".env");
			yield return Path.Combine(AppContext.BaseDirectory, ".env");
			yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"));
		}

		private static void LoadFile(string path)
		{
			foreach (var rawLine in File.ReadAllLines(path))
			{
				var line = rawLine.Trim();
				if (line.Length == 0 || line.StartsWith('#'))
				{
					continue;
				}

				var separatorIndex = line.IndexOf('=');
				if (separatorIndex <= 0)
				{
					continue;
				}

				var key = line[..separatorIndex].Trim();
				var value = line[(separatorIndex + 1)..].Trim();

				if ((value.StartsWith('"') && value.EndsWith('"')) ||
					(value.StartsWith('\'') && value.EndsWith('\'')))
				{
					value = value[1..^1];
				}

				if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
				{
					Environment.SetEnvironmentVariable(key, value);
				}
			}
		}
	}
}
