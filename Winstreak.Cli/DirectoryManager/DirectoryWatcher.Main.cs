using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Winstreak.Cli.Configuration;
using Winstreak.Cli.Utility.ConsoleTable;
using Winstreak.Core.Extensions;
using Winstreak.Core.Parsers.ImageParser;
using Winstreak.Core.Parsers.ImageParser.Imaging;
using static Winstreak.Core.WebApi.CachedData;
using Winstreak.Core.WebApi.Hypixel;
using Winstreak.Core.WebApi.Plancke;

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
			McScreenshotsPath = new DirectoryInfo(Path.Join(Config.PathToMinecraftFolder, "screenshots"));
			ShouldClearBeforeCheck = file.ClearConsole;

			if (file.HypixelApiKey != string.Empty)
			{
				HypixelApi = new HypixelApi(file.HypixelApiKey);

				var apiKeyValidationInfo = await HypixelApi.ValidateApiKeyAsync();
				ApiKeyValid = apiKeyValidationInfo.Success && apiKeyValidationInfo.Record != null;
				
				if (ApiKeyValid)
					HypixelApi.RequestsMade = apiKeyValidationInfo.Record!.QueriesInPastMin;
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

			var version = Assembly.GetEntryAssembly()?.GetName().Version;
			Console.WriteLine("%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%");
			Console.WriteLine($"Winstreak For Hypixel Bedwars");
			if (version != null)
				Console.WriteLine($"Version: {version}");
			Console.WriteLine("By CM19 & icicl");
#if DEBUG
			Console.WriteLine($"{AnsiConstants.TextBrightRedAnsi}Debug Mode!{AnsiConstants.ResetAnsi}");
#endif
			Console.WriteLine("%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%");

			Console.WriteLine();
			Console.WriteLine($"[INFO] Minecraft Folder Set: {Config.PathToMinecraftFolder}.");
			Console.WriteLine($"[INFO] {Config.DangerousPlayers.Length} Dangerous & {Config.ExemptPlayers.Length} Exempt Players Set.");
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
							Console.WriteLine($"[INFO] Delete Screenshot? {(file.DeleteScreenshot ? "Yes" : "No")}");
							Console.WriteLine($"[INFO] Checking Friends? {(ApiKeyValid && file.CheckFriends ? "Yes" : "No")}");
							Console.WriteLine();
							Console.WriteLine($"[INFO] Screenshot Delay Set: {Config.ScreenshotDelay} MS");
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
				var (profiles, nicked) = await PlanckeApi
					.GetMultipleProfilesFromPlancke(ignsToCheck);
				
				if (profiles.Count == 1)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"[{profiles[0].BedwarsLevel}] {profiles[0].Name}");
					Console.ResetColor();

					var eightOne = profiles[0].EightOneBedwarsStats;
					var eightOneKdr = eightOne.GetKdr();
					var eightOneFkdr = eightOne.GetFkdr();
					var eightOneWlr = eightOne.GetWinLossRatio();

					var eightTwo = profiles[0].EightTwoBedwarsStats;
					var eightTwoKdr = eightTwo.GetKdr();
					var eightTwoFkdr = eightTwo.GetFkdr();
					var eightTwoWlr = eightTwo.GetWinLossRatio();

					var fourThree = profiles[0].FourThreeBedwarsStats;
					var fourThreeKdr = fourThree.GetKdr();
					var fourThreeFkdr = fourThree.GetFkdr();
					var fourThreeWlr = fourThree.GetWinLossRatio();

					var fourFour = profiles[0].FourFourBedwarsStats;
					var fourFourKdr = fourFour.GetKdr();
					var fourFourFkdr = fourFour.GetFkdr();
					var fourFourWlr = fourFour.GetWinLossRatio();

					var overall = profiles[0].OverallBedwarsStats;
					var overallKdr = overall.GetKdr();
					var overallFkdr = overall.GetFkdr();
					var overallWlr = overall.GetWinLossRatio();

					var table = new Table(11)
						.AddRow("Type", "Kills", "Deaths", "KDR", "F Kills", "F Deaths", "FKDR", "Wins",
							"Losses", "WLR", "Beds")
						.AddSeparator()
						.AddRow("Solos", eightOne.Kills, eightOne.Deaths,
							eightOneKdr.dZero
								? "-"
								: Math.Round(eightOneKdr.kdr, 2) + "",
							eightOne.FinalKills, eightOne.FinalDeaths,
							eightOneFkdr.fdZero
								? "-"
								: Math.Round(eightOneFkdr.fkdr, 2) + "",
							eightOne.Wins, eightOne.Losses,
							eightOneWlr.lZero
								? "-"
								: Math.Round(eightOneWlr.wlr, 2) + "",
							eightOne.BrokenBeds)
						.AddSeparator()
						.AddRow("Doubles", eightTwo.Kills, eightTwo.Deaths,
							eightTwoKdr.dZero
								? "-"
								: Math.Round(eightTwoKdr.kdr, 2) + "",
							eightTwo.FinalKills, eightTwo.FinalDeaths,
							eightTwoFkdr.fdZero
								? "-"
								: Math.Round(eightTwoFkdr.fkdr, 2) + "",
							eightTwo.Wins, eightTwo.Losses,
							eightTwoWlr.lZero
								? "-"
								: Math.Round(eightTwoWlr.wlr, 2) + "",
							eightTwo.BrokenBeds)
						.AddSeparator()
						.AddRow("3v3v3v3", fourThree.Kills, fourThree.Deaths,
							fourThreeKdr.dZero
								? "-"
								: Math.Round(fourThreeKdr.kdr, 2) + "",
							fourThree.FinalKills, fourThree.FinalDeaths,
							fourThreeFkdr.fdZero
								? "-"
								: Math.Round(fourThreeFkdr.fkdr, 2) + "",
							fourThree.Wins, fourThree.Losses,
							fourThreeWlr.lZero
								? "-"
								: Math.Round(fourThreeWlr.wlr, 2) + "",
							fourThree.BrokenBeds)
						.AddSeparator()
						.AddRow("4v4v4v4", fourFour.Kills, fourFour.Deaths,
							fourFourKdr.dZero
								? "-"
								: Math.Round(fourFourKdr.kdr, 2) + "",
							fourFour.FinalKills, fourFour.FinalDeaths,
							fourFourFkdr.fdZero
								? "-"
								: Math.Round(fourFourFkdr.fkdr, 2) + "",
							fourFour.Wins, fourFour.Losses,
							fourFourWlr.lZero
								? "-"
								: Math.Round(fourFourWlr.wlr, 2) + "",
							fourFour.BrokenBeds)
						.AddSeparator()
						.AddRow("Overall", overall.Kills, overall.Deaths,
							overallKdr.dZero
								? "-"
								: Math.Round(overallKdr.kdr, 2) + "",
							overall.FinalKills, overall.FinalDeaths,
							overallFkdr.fdZero
								? "-"
								: Math.Round(overallFkdr.fkdr, 2) + "",
							overall.Wins, overall.Losses,
							overallWlr.lZero
								? "-"
								: Math.Round(overallWlr.wlr, 2) + "",
							overall.BrokenBeds);

					Console.WriteLine(table.ToString());
					Console.WriteLine($"> Winstreak: {profiles[0].Winstreak}");
					Console.WriteLine($"> Network Level: {profiles[0].NetworkLevel}");
					Console.WriteLine($"> Karma: {profiles[0].Karma}");
					Console.WriteLine($"> First Joined: {profiles[0].FirstJoined:MM/dd/yyyy hh:mm tt}");
				}
				else
				{
					var table = new Table(6)
						.AddRow("LVL", "Username", "FKDR", "Beds", "W/L", "WS")
						.AddSeparator();
					foreach (var bedwarsData in profiles)
					{
						table.AddRow(
							bedwarsData.BedwarsLevel,
							bedwarsData.Name,
							bedwarsData.OverallBedwarsStats.FinalDeaths == 0
								? "N/A"
								: Math.Round((double) bedwarsData.OverallBedwarsStats.FinalKills / bedwarsData.OverallBedwarsStats.FinalDeaths, 2)
									.ToString(CultureInfo.InvariantCulture),
							bedwarsData.OverallBedwarsStats.BrokenBeds,
							bedwarsData.OverallBedwarsStats.Losses == 0
								? "N/A"
								: Math.Round((double) bedwarsData.OverallBedwarsStats.Wins / bedwarsData.OverallBedwarsStats.Losses, 2)
									.ToString(CultureInfo.InvariantCulture),
							bedwarsData.Winstreak
						);
					}

					if (nicked.Count > 0)
					{
						if (profiles.Count > 0)
							table.AddSeparator();

						foreach (var erroredPlayer in nicked)
							table.AddRow(
								"N/A",
								erroredPlayer,
								"N/A",
								"N/A",
								"N/A",
								"N/A"
							);
					}

					Console.WriteLine(table.ToString());
				}

				checkTime.Stop();
				Console.WriteLine();
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
		private static async Task ProcessScreenshotAsync(Bitmap bitmap, string path, bool tryAgain = false)
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