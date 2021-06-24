#define USE_NEW_PARSER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Winstreak.Cli.Configuration;
using Winstreak.Cli.Utility;
using Winstreak.Cli.Utility.ConsoleTable;
using Winstreak.Core.LogReader;
using Winstreak.Core.Parsers.ImageParser;
using Winstreak.Core.Parsers.ImageParser.Imaging;

namespace Winstreak.Cli.DirectoryManager
{
	public static partial class DirectoryWatcher
	{
		/// <summary>
		/// The "main" entry point for the program. This is where the program will be executing from.
		/// </summary>
		/// <param name="file">The configuration file.</param>
		/// <returns>Nothing.</returns>
		[SuppressMessage("Microsoft.Style", "IDE0042")]
		public static async Task RunAsync(ConfigFile file)
		{
			// init vars
			Config = file;
			var pathToScreenshots = Path.Join(Config.PathToMinecraftFolder, "screenshots");
			McScreenshotsPath = Directory.Exists(pathToScreenshots)
				? new DirectoryInfo(Path.Join(Config.PathToMinecraftFolder, "screenshots"))
				: Directory.CreateDirectory(pathToScreenshots);
			ShouldClearBeforeCheck = file.ClearConsole;

			// Get gui scale
			GuiScale = ParserHelper.GetGuiScale(Config.PathToMinecraftFolder);

			if (GuiScale == 0)
			{
				OutputDisplayer.WriteLine(LogType.Error, "Please set a non-automatic " +
				                                         "GUI scale in your Minecraft settings " +
				                                         "and then restart the program.");
				return;
			}

			var version = Assembly.GetEntryAssembly()?.GetName().Version;
			Console.WriteLine("%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%");
			Console.WriteLine($"Winstreak For Hypixel Bedwars");
			if (version is not null)
				Console.WriteLine($"Version: {version}");
			Console.WriteLine("By CM19 & icicl");
#if DEBUG
			Console.WriteLine($"{AnsiConstants.TextBrightRedAnsi}Debug Mode!{AnsiConstants.ResetAnsi}");
#endif
			Console.WriteLine("%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%");

			OutputDisplayer.WriteLine(LogType.Info, "Attempting to connect to Hypixel's API...");
			var res = await ValidateApiKey(file.HypixelApiKey);
			OutputDisplayer.WriteLine(LogType.Info, res
				? "Connected to Hypixel's API."
				: "Unable to connect to Hypixel's API. Using Plancke.");
			OutputDisplayer.WriteLine(LogType.Info, "Ready.");
			Console.WriteLine(Divider);

			OutputDisplayer.WriteLine(LogType.Info, $"Minecraft Folder Set: {Config.PathToMinecraftFolder}");
			OutputDisplayer.WriteLine(LogType.Info, $"Logs Folder Set: {Config.PathToLogsFolder}");
			OutputDisplayer.WriteLine(LogType.Info, $"{Config.ExemptPlayers.Count} Exempt Players Set.");
			Console.WriteLine();
			OutputDisplayer.WriteLine(LogType.Info, "To use, simply take a screenshot in Minecraft by pressing F2.");
			OutputDisplayer.WriteLine(LogType.Info, "Need help? Type -h in here!");
			OutputDisplayer.WriteLine(LogType.Info, "To view current configuration, type -config in here!");
			Console.WriteLine(Divider);

			// make all lowercase for ease of comparison 
			Config.ExemptPlayers = Config.ExemptPlayers
				.Select(x => x.ToLower())
				.ToList();
			NamesInExempt = Config.ExemptPlayers.ToArray();

			// init watcher
			using var watcher = new FileSystemWatcher
			{
				Path = McScreenshotsPath.FullName,
				// Only watch image files
				Filter = "*.png",
				// Filters
				NotifyFilter = NotifyFilters.FileName,
				// Add event handlers.
				// Begin watching.
				EnableRaisingEvents = true
			};
			watcher.Created += OnChangedAsync;

			// Log time
			LogReader = new MinecraftLogReader(Config.PathToLogsFolder);
			LogReader.OnLogUpdate += LogUpdate;
			LogReader.Start();

			// infinite loop for command processing
			while (true)
			{
				if (!await RunCommandLoop())
					break;
			}
		}


		/// <summary>
		/// Method that is to be executed when a file is created.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="e">Arguments.</param>
		private static async void OnChangedAsync(object source, FileSystemEventArgs e)
			// wait for image to fully load
			=> await OnChangeFileAsync(e);

		/// <summary>
		/// Method that is to be executed when a file is created.
		/// </summary>
		/// <param name="e">The arguments.</param>
		/// <param name="init">Whether the method was executed by another method (true) or by itself (false).</param>
		/// <returns>Nothing.</returns>
		private static async Task OnChangeFileAsync(FileSystemEventArgs e, bool init = true)
		{
			await Task.Delay(Config.ScreenshotDelay);

			Bitmap bitmap;
			try
			{
				bitmap = new Bitmap(ImageHelper.FromFile(e.FullPath));
			}
			catch (IOException)
			{
				if (!init)
				{
					OutputDisplayer.WriteLine(LogType.Error, "Unable to read the image.");
					Console.WriteLine(Divider);
					return;
				}

				await Task.Delay(Config.ScreenshotDelay);
				await OnChangeFileAsync(e, false);
				return;
			}

			catch (Exception ex)
			{
				OutputDisplayer.WriteLine(LogType.Error, $"An unknown error occurred. Error Information:\n{ex}");
				Console.WriteLine(Divider);
				return;
			}

			await ProcessScreenshotAsync(bitmap, e.FullPath);
			bitmap.Dispose();
		}

		/// <summary>
		/// Processes the screenshot that was provided.
		/// </summary>
		/// <param name="bitmap">The screenshot as a Bitmap.</param>
		/// <param name="path">The name of the screenshot.</param>
		/// <returns>Nothing.</returns>
		private static async Task ProcessScreenshotAsync(Bitmap bitmap, string path)
		{
			var fileInfo = new FileInfo(path);
			if (ShouldClearBeforeCheck)
				Console.Clear();

			OutputDisplayer.WriteLine(LogType.Info, $"Checking Screenshot: {fileInfo.Name}");
			var processingTime = new Stopwatch();
			processingTime.Start();
			// parse time
#if USE_NEW_PARSER
			using INameParser parser = new EnhancedNameParser(bitmap, GuiScale, Config.StrictParser);
#else
			using INameParser parser = new NameParser(bitmap, GuiScale);
#endif
			var allNames = parser.ParseNames(Config.ExemptPlayers);
			// Remove all '[ ]'
			var parsedNames = new Dictionary<TeamColor, IList<string>>();
			foreach (var (color, names) in allNames)
			{
				parsedNames.Add(color, new List<string>());
				foreach (var name in names)
				{
					if (!name.Contains('[') && !name.Contains(']'))
					{
						parsedNames[color].Add(name);
						continue;
					}

					// If it contains [ or ] but not both, then it's defective. 
					if (name.Contains('[') ^ name.Contains(']'))
						continue;

					var nameNoTag = name.Split("]")[1];
					if (nameNoTag.Contains("["))
						nameNoTag = nameNoTag.Split("[")[0];

					nameNoTag = nameNoTag.Trim();
					if (nameNoTag == string.Empty)
						continue;

					parsedNames[color].Add(nameNoTag);
				}
			}

			if (parsedNames.Count == 0)
			{
				OutputDisplayer.WriteLine(LogType.Info, "No parseable names found. Skipping.");
				Console.WriteLine(Divider);
				return;
			}

			// end parse
			processingTime.Stop();
			var timeTaken = processingTime.Elapsed;
			var parsedPeople = parsedNames.Sum(x => x.Value.Count);
			var entries = new[]
			{
				$"Type: {(parser.IsLobby ? "Lobby" : "Game")}",
				$"Players: {parsedPeople}"
			};
			Console.WriteLine("[" + string.Join(", ", entries) + "]");

			if (HypixelApi is not null && ApiKeyValid)
			{
				var res = await HypixelApi.ValidateApiKeyAsync();
				if (!res.Success)
				{
					HypixelApi = null;
					ApiKeyValid = false;
				}
			}

			if (parser.IsLobby)
			{
				if (parsedNames.ContainsKey(TeamColor.Unknown))
					await ProcessLobbyScreenshotAsync(parsedNames[TeamColor.Unknown], timeTaken);
				else
				{
					OutputDisplayer.WriteLine(LogType.Error,
						"An error occurred with the parse results. Please take another screenshot.");
					Console.WriteLine(Divider);
				}
			}
			else
				await ProcessInGameScreenshotAsync(parsedNames, timeTaken);

#if !DEBUG
			if (Config.DeleteScreenshot)
				fileInfo.Delete();
#endif
		}
	}
}