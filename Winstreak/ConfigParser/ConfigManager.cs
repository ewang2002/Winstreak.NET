﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Winstreak.ConfigParser
{
	public static class ConfigManager
	{
		public static async Task<ConfigFile> ParseConfigFile(FileInfo info)
		{
			var configFile = new ConfigFile();

			var lines = await File.ReadAllLinesAsync(info.FullName);
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
					case "EXEMPT_PLAYERS":
						configFile.ExemptPlayers = val.Split(",").Select(x => x.Trim()).ToArray();
						break;
					case "RETRY_DELAY":
						configFile.RetryDelay = int.TryParse(val, out var v1)
							? v1 <= 100
								? 100
								: v1
							: 250;
						break;
					case "RETRY_MAX":
						configFile.RetryMax = int.TryParse(val, out var v2)
							? v2 <= 0
								? 0
								: v2
							: 2;
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
					case "GAMEMODE_TYPE":
						configFile.GamemodeType = int.TryParse(val, out var v5)
							? v5 == 34 || v5 == 12
								? v5
								: 34
							: 34;
						break;
					case "HYPIXEL_API_KEY":
						configFile.HypixelApiKey = val;
						break;
				}
			}

			return configFile;
		}
	}
}