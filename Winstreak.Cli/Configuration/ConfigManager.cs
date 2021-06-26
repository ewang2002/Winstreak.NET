using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Winstreak.Cli.DirectoryManager;

namespace Winstreak.Cli.Configuration
{
	public static class ConfigManager
	{
		/// <summary>
		/// Parses a configuration file.
		/// </summary>
		/// <param name="info">The file to read from.</param>
		/// <returns>The parsed configuration file.</returns>
		public static async Task<ConfigFile> ParseConfigFile(FileInfo info)
		{
			var configFile = new ConfigFile
			{
				FileData = info,
				ClearConsole = true,
				PathToLogsFolder = string.Empty,
				ExemptPlayers = new List<string>(),
				HypixelApiKey = string.Empty,
				PathToMinecraftFolder = string.Empty,
				ScreenshotDelay = 250,
				DeleteScreenshot = false,
				CheckFriends = true,
				SuppressErrorMessages = false,
				StrictParser = false,
				YourIgn = string.Empty
			};

			var lines = await File.ReadAllLinesAsync(info.FullName);
			DirectoryWatcher.ConfigRaw = lines;
			lines = lines
				.Where(x => !x.StartsWith('#'))
				.ToArray();

			foreach (var line in lines)
			{
				if (line.IndexOf('=') == -1)
					continue;

				var propVal = line.Split('=')
					.Select(x => x.Trim())
					.Where(x => x != string.Empty)
					.ToArray();
				if (propVal.Length < 2)
					continue;

				var prop = propVal[0].Trim();
				var val = string.Join('=', propVal.Skip(1)).Trim();
				switch (prop)
				{
					case "PATH_TO_MC_FOLDER" when Directory.Exists(val):
						configFile.PathToMinecraftFolder = val;
						break;
					case "PATH_TO_LOGS_FOLDER" when Directory.Exists(val):
						configFile.PathToLogsFolder = val;
						break;
					case "EXEMPT_PLAYERS":
						configFile.ExemptPlayers = val.Split(",").Select(x => x.Trim()).ToList();
						break;
					case "CLEAR_CONSOLE":
						configFile.ClearConsole = int.TryParse(val, out var v3) && v3 == 1;
						break;
					case "SCREENSHOT_DELAY":
						configFile.ScreenshotDelay = int.TryParse(val, out var v4)
							? v4 <= 250
								? 250
								: v4
							: 250;
						break;
					case "HYPIXEL_API_KEY":
						configFile.HypixelApiKey = val;
						break;
					case "DELETE_SCREENSHOT":
						configFile.DeleteScreenshot = int.TryParse(val, out var v6) && v6 == 1;
						break;
					case "CHECK_FRIENDS":
						configFile.CheckFriends = int.TryParse(val, out var v7) && v7 == 1;
						break;
					case "SUPPRESS_ERROR_MSGS":
						configFile.SuppressErrorMessages = int.TryParse(val, out var v8) && v8 == 1;
						break;
					case "PARSER_STRICT":
						configFile.StrictParser = int.TryParse(val, out var v9) && v9 == 1;
						break;
					case "YOUR_IGN":
						configFile.YourIgn = val;
						break;
				}
			}

			return configFile;
		}
	}
}