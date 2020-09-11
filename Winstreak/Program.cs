using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Winstreak.ConfigParser;

namespace Winstreak
{
	public class Program
	{
		public static async Task Main()
		{
			Console.Clear();
			// get configuration file
			var possiblePaths = new[]
			{
				Environment.CurrentDirectory,
				Path.GetDirectoryName((Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location),
			};

			var envPath = new DirectoryInfo(Environment.CurrentDirectory);
			var configFileInfo = (from path in possiblePaths
				where Directory.Exists(path)
				select envPath.GetFiles()
					.Where(x => x.Name.ToLower() == "wsconfig.txt")
					.ToArray()
				into possConfigArr
				where possConfigArr.Length != 0
				select possConfigArr[0]).FirstOrDefault();

			// default values
			var configurationFile = new ConfigFile
			{
				HypixelApiKey = string.Empty,
				GamemodeType = 34,
				ClearConsole = false,
				ExemptPlayers = new string[0],
				RetryDelay = 250,
				RetryMax = 2,
				ScreenshotDelay = 250,
				PathToMinecraftFolder = GetDefaultMinecraftFolderPath(),
				DangerousPlayers = new string[0]
			};

			if (configFileInfo != null)
				configurationFile = await ConfigManager.ParseConfigFile(configFileInfo);
			else
			{
				Console.WriteLine(
					"[INFO] A WSConfig file couldn't be found. Please type the path to the folder containing this file. If you would like to use the default settings, simply skip.");
				var pathToCheck = Console.ReadLine() ?? string.Empty;
				if (pathToCheck == string.Empty)
					Console.WriteLine("[INFO] No path specified. Using default settings.");
				else if (Directory.Exists(pathToCheck))
				{
					var dir = new DirectoryInfo(pathToCheck);
					var possFiles = dir.GetFiles()
						.Where(x => x.Name == "wsconfig.txt")
						.ToArray();
					if (possFiles.Length != 0)
						configurationFile = await ConfigManager.ParseConfigFile(possFiles[0]);
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("[ERROR] Couldn't find a configuration file. Using default settings.");
						Console.ResetColor();
					}
				}
			}

			if (string.IsNullOrEmpty(configurationFile.PathToMinecraftFolder))
				configurationFile.PathToMinecraftFolder = GetDefaultMinecraftFolderPath();

			// check once more
			if (!Directory.Exists(configurationFile.PathToMinecraftFolder))
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(
					$"[ERROR] Couldn't find your Minecraft folder. Given parameter: {configurationFile.PathToMinecraftFolder}");
				Console.ResetColor();
				return;
			}

			await DirectoryWatcher.Run(configurationFile);
			Console.WriteLine("[INFO] Program has been terminated. Press ENTER to close this program.");
			Console.ReadLine();
		}

		public static string GetDefaultMinecraftFolderPath() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? Path.Join("C:", "Users", Environment.UserName, "AppData", "Roaming", ".minecraft")
			: RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
				? Path.Join("home", Environment.UserName, ".minecraft")
				: RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
					? Path.Join("Library", "Application Support", "minecraft")
					: throw new PlatformNotSupportedException("Winstreak isn't supported by the current platform.");
	}
}