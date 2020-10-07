using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Winstreak.Extensions;
using Winstreak.Parsers.ConfigParser;
using Winstreak.Parsers.ImageParser;
using Winstreak.Parsers.ImageParser.Imaging;
using Winstreak.Utility.ConsoleTable;
using Winstreak.WebApi.Hypixel;
using Winstreak.WebApi.Plancke;
using Winstreak.WebApi.Plancke.Checker;
using static Winstreak.WebApi.ApiConstants;

namespace Winstreak.DirectoryManager
{
	public static partial class DirectoryWatcher
	{
		/// <summary>
		/// The "main" entry point for the program. This is where the program will be executing from.
		/// </summary>
		/// <param name="file">The configuration file.</param>
		/// <returns>Nothing.</returns>
		public static async Task RunAsync(ConfigFile file)
		{
			// init vars
			Config = file;
			McScreenshotsPath = new DirectoryInfo(Path.Join(Config.PathToMinecraftFolder, "screenshots"));
			ShouldClearBeforeCheck = file.ClearConsole;
			Mode = file.GamemodeType;

			if (file.HypixelApiKey != string.Empty)
			{
				HypixelApi = new HypixelApi(file.HypixelApiKey);
				ApiKeyValid = await HypixelApi.ValidateApiKeyAsync();
			}

			// Get gui scale
			GuiScale = ParserHelper.GetGuiScale(Config.PathToMinecraftFolder);

			if (GuiScale == 0)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(
					"[ERROR] Please set a non-automatic GUI scale in your Minecraft settings and then restart the program.");
				Console.ResetColor();
				return;
			}

			Console.WriteLine($"[INFO] Minecraft Folder Set: {Config.PathToMinecraftFolder}");
			Console.WriteLine($"[INFO] Dangerous Players Set: {Config.DangerousPlayers.ToReadableString()}");
			Console.WriteLine($"[INFO] Exempt Players Set: {Config.ExemptPlayers.ToReadableString()}");
			Console.WriteLine();
			Console.WriteLine("[INFO] To use, simply take a screenshot in Minecraft by pressing F2.");
			Console.WriteLine("[INFO] Need help? Type -h in here!");
			Console.WriteLine("[INFO] To view current configuration, type -config in here!");
			Console.WriteLine("=========================");

			// make all lowercase for ease of comparison 
			Config.ExemptPlayers = Config.ExemptPlayers
				.Select(x => x.ToLower())
				.ToArray();
			Config.DangerousPlayers = Config.DangerousPlayers
				.Select(x => x.ToLower())
				.ToArray();

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

			// infinite loop for command processing
			while (true)
			{
				var input = (Console.ReadLine() ?? string.Empty).Trim();
				if (input == string.Empty)
					continue;

				if (input.StartsWith('-'))
				{
					// quit program
					if (input.ToLower() == "-q" || input.ToLower() == "-quit")
						break;

					switch (input.ToLower().Trim())
					{
						case "-config":
							Console.WriteLine($"[INFO] Minecraft Folder Set: {Config.PathToMinecraftFolder}");
							Console.WriteLine($"[INFO] Dangerous Players Set: {Config.DangerousPlayers.ToReadableString()}");
							Console.WriteLine($"[INFO] Exempt Players Set: {Config.ExemptPlayers.ToReadableString()}");
							Console.WriteLine(
								$"[INFO] Using Hypixel API: {(ApiKeyValid ? "Yes" : "No")}");
							Console.WriteLine($"[INFO] Gamemode Set: {GamemodeIntToStr()}");
							Console.WriteLine($"[INFO] Delete Screenshot? {(file.DeleteScreenshot ? "Yes" : "No")}");
							Console.WriteLine($"[INFO] Checking Friends? {(ApiKeyValid && file.CheckFriends ? "Yes" : "No")}");
							Console.WriteLine();
							Console.WriteLine($"[INFO] Screenshot Delay Set: {Config.ScreenshotDelay} MS");
							Console.WriteLine($"[INFO] Retry Request Delay Set: {Config.RetryDelay} MS");
							Console.WriteLine($"[INFO] Retry Request Max Set: {Config.RetryMax}");
							Console.WriteLine($"[INFO] Using Gui Scale: {GuiScale}");
							Console.WriteLine(Divider);
							continue;
						case "-help":
						case "-h":
							Console.WriteLine(HelpInfo);
							Console.WriteLine(Divider);
							continue;
						case "-clear":
						case "-c":
							Console.Clear();
							continue;
						case "-gamemode":
						case "-gm":
							Mode = Mode == 34 ? 12 : 34;
							Console.WriteLine($"[INFO] Set parser gamemode to: {GamemodeIntToStr()}");
							Console.WriteLine(Divider);
							continue;
						case "-tc":
							ShouldClearBeforeCheck = !ShouldClearBeforeCheck;
							Console.WriteLine(ShouldClearBeforeCheck
								? "[INFO] Console will be cleared once a screenshot is provided."
								: "[INFO] Console will not be cleared once a screenshot is provided.");
							Console.WriteLine(Divider);
							continue;
						case "-status":
							var valid = HypixelApi != null && ApiKeyValid;
							Console.WriteLine(
								$"[INFO] Hypixel API: {(valid ? "Valid" : "Invalid")}");
							Console.WriteLine(valid
								? $"[INFO] Usage: {HypixelApi.RequestsMade}/{HypixelApi.MaximumRequestsInRateLimit}"
								: "[INFO] Usage: Unlimited (Plancke)");
							Console.WriteLine($"[INFO] Player Cache Length: {CachedPlayerData.Length}");
							Console.WriteLine($"[INFO] Friend Cache Length: {CachedFriendsData.Length}");
							Console.WriteLine($"[INFO] Sort Mode: {SortingType}");
							Console.WriteLine(Divider);
							continue;
						case "-emptycache":
							Console.WriteLine("[INFO] Cache has been cleared.");
							CachedPlayerData.Empty();
							CachedFriendsData.Empty();
							CachedGuildData.Empty();
							Console.WriteLine(Divider);
							continue;
						case "-sortmode":
						case "-sort":
						case "-s":
							SortingType = SortingType switch
							{
								SortType.Score => SortType.Beds,
								SortType.Beds => SortType.Finals,
								SortType.Finals => SortType.Fkdr,
								SortType.Fkdr => SortType.Winstreak,
								SortType.Winstreak => SortType.Level,
								_ => SortType.Score
							};

							Console.WriteLine($"[INFO] Sorting By: {SortingType}");
							Console.WriteLine(Divider);
							continue;
					}

					Console.WriteLine(HelpInfo);
					Console.WriteLine(Divider);
					continue;
				}

				if (input.Contains('-') || input.Contains('\\'))
					continue;

				var ignsToCheck = input.Split(" ")
					.Select(x => x.Trim())
					.Where(x => x != string.Empty)
					.ToList();

				// check ign
				var checkTime = new Stopwatch();
				checkTime.Start();
				var requester = new PlanckeApiRequester(ignsToCheck);
				var results = await requester.SendRequestsAsync();
				var respPar = new ResponseParser(results);
				var data = respPar.GetPlayerDataFromMap();

				if (data.Count == 1)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($@"[INFO] ""{input}"" Found!");
					Console.ResetColor();
					Console.WriteLine($"> Broken Beds: {data[0].BrokenBeds}");
					Console.WriteLine($"> Final Kills: {data[0].FinalKills}");
					Console.WriteLine($"> Final Deaths: {data[0].FinalDeaths}");
					Console.WriteLine($"> Total Wins: {data[0].Wins}");
					Console.WriteLine($"> Total Losses: {data[0].Losses}");
					Console.WriteLine();
					Console.WriteLine($"> Regular K/D Ratio: {(double) data[0].Kills / data[0].Deaths}");
					Console.WriteLine($"> Final K/D Ratio: {(double) data[0].FinalKills / data[0].FinalDeaths}");
					Console.WriteLine($"> W/L Ratio: {(double) data[0].Wins / data[0].Losses}");
					Console.WriteLine($"> Winstreak: {data[0].Winstreak}");
				}
				else
				{
					var table = new Table(6)
						.AddRow("LVL", "Username", "FKDR", "Beds", "W/L", "WS")
						.AddSeparator();
					foreach (var bedwarsData in data)
					{
						table.AddRow(
							bedwarsData.Level,
							bedwarsData.Name,
							bedwarsData.FinalDeaths == 0
								? "N/A"
								: Math.Round((double) bedwarsData.FinalKills / bedwarsData.FinalDeaths, 2)
									.ToString(CultureInfo.InvariantCulture),
							bedwarsData.BrokenBeds,
							bedwarsData.Losses == 0
								? "N/A"
								: Math.Round((double) bedwarsData.Wins / bedwarsData.Losses, 2)
									.ToString(CultureInfo.InvariantCulture),
							bedwarsData.Winstreak
						);
					}

					if (respPar.ErroredPlayers.Count > 0)
					{
						table.AddSeparator();
						foreach (var erroredPlayer in respPar.ErroredPlayers)
						{
							table.AddRow(
								"N/A",
								erroredPlayer,
								"N/A",
								"N/A",
								"N/A",
								"N/A"
							);
						}
					}

					Console.WriteLine(table.ToString());
				}

				checkTime.Stop();
				Console.WriteLine($"> Time Taken: {checkTime.Elapsed.TotalSeconds} Seconds.");
				Console.WriteLine(Divider);
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
			catch (IOException ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"[ERROR] An IOException occurred. Error Information:\n{ex}");
				Console.WriteLine(init ? "\tTrying Again." : "\tNo Longer Trying Again.");
				Console.ResetColor();
				Console.WriteLine(Divider);
				if (!init)
					return;
				await Task.Delay(Config.ScreenshotDelay);
				await OnChangeFileAsync(e, false);
				return;
			}

			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"[ERROR] An unknown error occurred. Error Information:\n{ex}");
				Console.ResetColor();
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
		/// <param name="path">The path to the screenshot.</param>
		/// <returns>Nothing.</returns>
		private static async Task ProcessScreenshotAsync(Bitmap bitmap, string path)
		{
			if (ShouldClearBeforeCheck)
				Console.Clear();

			Console.WriteLine($"[INFO] Checking Screenshot: {path}");
			var processingTime = new Stopwatch();
			processingTime.Start();
			// parse time
			using var parser = new NameParser(bitmap);
			try

			{
				parser.SetGameMode(Mode);
				parser.SetGuiScale(GuiScale);
				parser.InitPoints();
				parser.FindStartOfName();
			}
			catch (Exception e)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(
					$"[ERROR] An error occurred when trying to parse the image. Exception Info Below.\n{e}");
				Console.ResetColor();
				Console.WriteLine(Divider);
				processingTime.Stop();
				return;
			}

			var allNames = parser.ParseNames(Config.ExemptPlayers);

			// end parse
			processingTime.Stop();
			var timeTaken = processingTime.Elapsed;
			Console.WriteLine($"[INFO] Determined Screenshot Type: {(parser.IsLobby ? "Lobby" : "Game")}");
			if (parser.IsLobby)
			{
				if (allNames.ContainsKey(TeamColor.Unknown))
					await ProcessLobbyScreenshotAsync(allNames[TeamColor.Unknown], timeTaken);
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine(
						"[ERROR] An error occurred with the result of the parsing. Please take another screenshot.");
					Console.ResetColor();
				}
			}
			else
				await ProcessInGameScreenshotAsync(allNames, timeTaken);

			if (Config.DeleteScreenshot)
				File.Delete(path);
		}
	}
}