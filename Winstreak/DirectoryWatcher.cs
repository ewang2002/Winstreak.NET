using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Winstreak.Extensions;
using Winstreak.Parsers.ConfigParser;
using Winstreak.Parsers.ImageParser;
using Winstreak.Parsers.ImageParser.Imaging;
using Winstreak.Utility.Calculations;
using Winstreak.Utility.ConsoleTable;
using Winstreak.WebApi;
using Winstreak.WebApi.Definition;
using Winstreak.WebApi.Hypixel;
using Winstreak.WebApi.Plancke;
using static Winstreak.WebApi.ApiConstants;
using Winstreak.WebApi.Plancke.Checker;
using static Winstreak.Utility.ConsoleTable.AnsiConstants;

namespace Winstreak
{
	public class DirectoryWatcher
	{
		public static string HelpInfo = new StringBuilder()
			.Append("[INFO] Current Command List.")
			.AppendLine()
			.Append("> -clear OR -c: Clears the console.")
			.AppendLine()
			.Append(
				"> -tc: Determines whether the console should be cleared when a screenshot is provided.")
			.AppendLine()
			.Append("> -status: Views program status.")
			.AppendLine()
			.Append("> -emptycache: Empties the cache.")
			.AppendLine()
			.Append("> -gamemode OR -gm: Switches the parser gamemode from solos/doubles to 3s/4s or vice versa.")
			.AppendLine()
			.Append("> -sortmode OR -sort OR -s: Changes the way the program sorts the presented data. By default, this is set to the Score value.")
			.AppendLine()
			.Append("> -help OR -h: Shows this menu.")
			.AppendLine()
			.Append("> -quit OR -q: Quits the program.")
			.ToString();

		public static readonly string Divider = "=====================================";

		public static DirectoryInfo McScreenshotsPath;

		public static int GuiScale;
		public static bool ShouldClearBeforeCheck;

		public static ConfigFile Config;
		public static int Mode = 34;

		public static HypixelApi HypixelApi;
		public static bool ApiKeyValid;

		public static SortType SortingType = SortType.Score;

		public static async Task Run(ConfigFile file)
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
			Console.WriteLine(
				$"[INFO] Using Hypixel API: {(file.HypixelApiKey != string.Empty && ApiKeyValid ? "Yes" : "No")}");
			Console.WriteLine($"[INFO] Gamemode Set: {GamemodeIntToStr()}");
			Console.WriteLine($"[INFO] Delete Screenshot? {(file.DeleteScreenshot ? "Yes" : "No")}");
			Console.WriteLine();
			Console.WriteLine($"[INFO] Screenshot Delay Set: {Config.ScreenshotDelay} MS");
			Console.WriteLine($"[INFO] Retry Request Delay Set: {Config.RetryDelay} MS");
			Console.WriteLine($"[INFO] Retry Request Max Set: {Config.RetryMax}");
			Console.WriteLine($"[INFO] Using Gui Scale: {GuiScale}");
			Console.WriteLine();
			Console.WriteLine("[INFO] To use, simply take a screenshot in Minecraft by pressing F2.");
			Console.WriteLine("[INFO] Need help? Type -h in here!");
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
			watcher.Created += OnChanged;

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
							Console.WriteLine($"[INFO] Hypixel API: {(HypixelApi != null && ApiKeyValid ? "Valid" : "Invalid")}");
							Console.WriteLine(valid
								? $"[INFO] Usage: {HypixelApi.RequestsMade}/{HypixelApi.MaximumRequestsInRateLimit}"
								: "[INFO] Usage: Unlimited (Plancke)");
							Console.WriteLine($"[INFO] Cache Length: {CachedPlayerData.Length}");
							Console.WriteLine($"[INFO] Sort Mode: {SortingType}");
							Console.WriteLine(Divider);
							continue;
						case "-emptycache":
							Console.WriteLine("[INFO] Cache has been cleared.");
							CachedPlayerData.Empty();
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

				if (input.Contains('-'))
					continue;

				// check ign
				var checkTime = new Stopwatch();
				checkTime.Start();
				var results = await ApiConstants.ApiClient
					.GetAsync($"https://plancke.io/hypixel/player/stats/{input}");
				var responseHtml = await results.Content.ReadAsStringAsync();
				var data = new ResponseData(input, responseHtml)
					.Parse();
				if (data.TotalDataInfo is { } playerInfo)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($@"[INFO] ""{input}"" Found!");
					Console.ResetColor();
					Console.WriteLine($"> Broken Beds: {playerInfo.BrokenBeds}");
					Console.WriteLine($"> Final Kills: {playerInfo.FinalKills}");
					Console.WriteLine($"> Final Deaths: {playerInfo.FinalDeaths}");
					Console.WriteLine($"> Total Wins: {playerInfo.Wins}");
					Console.WriteLine($"> Total Losses: {playerInfo.Losses}");
					Console.WriteLine();
					Console.WriteLine($"> Regular K/D Ratio: {(double) playerInfo.Kills / playerInfo.Deaths}");
					Console.WriteLine($"> Final K/D Ratio: {(double) playerInfo.FinalKills / playerInfo.FinalDeaths}");
					Console.WriteLine($"> W/L Ratio: {(double) playerInfo.Wins / playerInfo.Losses}");
					Console.WriteLine($"> Winstreak: {playerInfo.Winstreak}");
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($@"[INFO] ""{input}"" Not Found!");
					Console.ResetColor();
				}

				checkTime.Stop();
				Console.WriteLine($"> Time Taken: {checkTime.Elapsed.TotalSeconds} Seconds.");
				Console.WriteLine(Divider);
			}
		}

		private static async void OnChanged(object source, FileSystemEventArgs e)
			// wait for image to fully load
			=> await OnChangeFile(e);

		private static async Task OnChangeFile(FileSystemEventArgs e, bool init = true)
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
				await OnChangeFile(e, false);
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

			await ProcessScreenshot(bitmap, e.FullPath);
			bitmap.Dispose();
		}

		private static async Task ProcessScreenshot(Bitmap bitmap, string path)
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
				await LobbyChecker(allNames[TeamColor.Unknown], timeTaken);
			else
				await GameCheck(allNames, timeTaken);

			if (Config.DeleteScreenshot)
				File.Delete(path);
		}

		private static async Task LobbyChecker(IEnumerable<string> names, TimeSpan timeTaken)
		{
			var reqTime = new Stopwatch();
			reqTime.Start();
			var nickedPlayers = new List<string>();
			var totalWins = 0;
			var totalLosses = 0;
			var totalFinalKills = 0;
			var totalFinalDeaths = 0;
			var totalBrokenBeds = 0;
			var levels = 0;

			// we assume this checks
			// the entire cache
			// so no need to check cache again
			// except to add more people
			var nameResults = new List<BedwarsData>();
			var namesToCheck = new List<string>();
			foreach (var name in names)
			{
				if (!CachedPlayerData.Contains(name))
				{
					namesToCheck.Add(name);
					continue;
				}

				var data = CachedPlayerData[name];
				nameResults.Add(data);

				totalWins += data.Wins;
				totalLosses += data.Losses;
				totalFinalKills += data.FinalKills;
				totalFinalDeaths += data.FinalDeaths;
				totalBrokenBeds += data.BrokenBeds;

				if (data.Level != -1)
					levels += data.Level;
			}

			// check hypixel api
			if (HypixelApi != null && ApiKeyValid)
			{
				var (responses, nicked, unableToSearch) = await HypixelApi
					.GetAllPlayersAsync(namesToCheck);
				nickedPlayers = nicked.ToList();

				foreach (var resp in responses)
				{
					totalWins += resp.Wins;
					totalLosses += resp.Losses;
					totalBrokenBeds += resp.BrokenBeds;
					totalFinalKills += resp.FinalKills;
					totalFinalDeaths += resp.FinalDeaths;
					levels += resp.Level;

					CachedPlayerData.TryAdd(resp.Name, resp);
					nameResults.Add(resp);
				}

				// request leftover data from plancke
				var planckeApiRequester = new PlanckeApiRequester(unableToSearch);
				// parse data
				var nameData = await planckeApiRequester
					.SendRequests();
				var checker = new ResponseParser(nameData);

				foreach (var playerInfo in checker.GetPlayerDataFromMap())
				{
					totalFinalDeaths += playerInfo.FinalDeaths;
					totalFinalKills += playerInfo.FinalKills;
					totalBrokenBeds += playerInfo.BrokenBeds;
					totalWins += playerInfo.Wins;
					totalLosses += playerInfo.Losses;
					if (playerInfo.Level != -1)
						levels += playerInfo.Level;

					CachedPlayerData.TryAdd(playerInfo.Name, playerInfo);
					nameResults.Add(playerInfo);
				}

				nickedPlayers.AddRange(checker.ErroredPlayers);
			}
			else
			{
				// request data from plancke
				var planckeApiRequester = new PlanckeApiRequester(namesToCheck);
				// parse data
				var nameData = await planckeApiRequester
					.SendRequests();
				var checker = new ResponseParser(nameData);

				foreach (var playerInfo in checker.GetPlayerDataFromMap())
				{
					totalFinalDeaths += playerInfo.FinalDeaths;
					totalFinalKills += playerInfo.FinalKills;
					totalBrokenBeds += playerInfo.BrokenBeds;
					totalWins += playerInfo.Wins;
					totalLosses += playerInfo.Losses;
					if (playerInfo.Level != -1)
						levels += playerInfo.Level;

					CachedPlayerData.TryAdd(playerInfo.Name, playerInfo);
					nameResults.Add(playerInfo);
				}

				nickedPlayers.AddRange(checker.ErroredPlayers.ToList());
			}

			nameResults = nameResults
				.OrderByDescending(SortBySpecifiedType())
				.ToList();

			reqTime.Stop();
			var apiRequestTime = reqTime.Elapsed;
			reqTime.Reset();

			// start parsing the data
			var tableBuilder = new Table(8)
				.AddRow("LVL", "Username", "Finals", "Beds", "FKDR", "WS", "Score", "Assessment")
				.AddSeparator();
			foreach (var playerInfo in nameResults)
				tableBuilder.AddRow(
					playerInfo.Level == -1 ? "N/A" : playerInfo.Level.ToString(),
					Config.DangerousPlayers.Contains(playerInfo.Name.ToLower())
						? BackgroundBrightYellowAnsi + playerInfo.Name + ResetAnsi
						: playerInfo.Name,
					playerInfo.FinalKills,
					playerInfo.BrokenBeds,
					playerInfo.FinalDeaths == 0
						? "N/A"
						: Math.Round((double) playerInfo.FinalKills / playerInfo.FinalDeaths, 2)
							.ToString(CultureInfo.InvariantCulture),
					playerInfo.Winstreak == -1
						? "N/A"
						: playerInfo.Winstreak.ToString(),
					Math.Round(playerInfo.Score, 2),
					DetermineScoreMeaning(playerInfo.Score, true)
				);

			foreach (var nickedPlayer in nickedPlayers)
				tableBuilder.AddRow(
					BackgroundRedAnsi + "N/A" + ResetAnsi,
					BackgroundRedAnsi + nickedPlayer + ResetAnsi,
					BackgroundRedAnsi + "N/A" + ResetAnsi,
					BackgroundRedAnsi + "N/A" + ResetAnsi,
					BackgroundRedAnsi + "N/A" + ResetAnsi,
					BackgroundRedAnsi + "N/A" + ResetAnsi,
					BackgroundRedAnsi + "N/A" + ResetAnsi,
					BackgroundRedAnsi + "Nicked!" + ResetAnsi
				);

			tableBuilder.AddSeparator();
			var ttlScore = PlayerCalculator.CalculatePlayerThreatLevel(totalWins, totalLosses,
				totalFinalKills, totalFinalDeaths, totalBrokenBeds);
			tableBuilder.AddRow(
				levels,
				"Total",
				totalFinalKills,
				totalBrokenBeds,
				totalLosses == 0
					? "N/A"
					: Math.Round((double) totalWins / totalLosses, 2)
						.ToString(CultureInfo.InvariantCulture),
				string.Empty,
				Math.Round(ttlScore, 2),
				DetermineScoreMeaning(ttlScore, false)
			);

			Console.WriteLine(tableBuilder.ToString());
			Console.WriteLine($"[INFO] Image Processing Time: {timeTaken.TotalSeconds} Sec.");
			Console.WriteLine($"[INFO] API Requests Time: {apiRequestTime.TotalSeconds} Sec.");
			Console.WriteLine(Divider);
		}

		public static async Task GameCheck(IDictionary<TeamColor, IList<string>> teams, TimeSpan timeTaken)
		{
			var reqTime = new Stopwatch();
			reqTime.Start();
			// req data from plancke 
			var teamInfo = new List<TeamInfoResults>();
			foreach (var (key, value) in teams)
			{
				var actualNamesToCheck = new List<string>();
				var teamStats = new List<BedwarsData>();
				foreach (var name in value)
				{
					if (!CachedPlayerData.Contains(name))
					{
						actualNamesToCheck.Add(name);
						continue;
					}

					teamStats.Add(CachedPlayerData[name]);
				}

				var nickedPlayers = new List<string>();

				if (HypixelApi != null && ApiKeyValid)
				{
					var (responses, nicked, unableToSearch) = await HypixelApi.GetAllPlayersAsync(actualNamesToCheck);

					foreach (var data in responses)
					{
						if (!CachedPlayerData.Contains(data.Name))
							CachedPlayerData.TryAdd(data.Name, data);
						teamStats.Add(data);
					}

					nickedPlayers.AddRange(nicked);

					if (unableToSearch.Count != 0)
					{
						var planckeApiRequester = new PlanckeApiRequester(actualNamesToCheck);
						var teamData = await planckeApiRequester
							.SendRequests();
						var p = new ResponseParser(teamData);
						foreach (var playerInfo in p.GetPlayerDataFromMap())
						{
							CachedPlayerData.TryAdd(playerInfo.Name, playerInfo);
							teamStats.Add(playerInfo);
						}

						nickedPlayers.AddRange(p.ErroredPlayers);
					}
				}
				else
				{
					var planckeApiRequester = new PlanckeApiRequester(actualNamesToCheck);
					// parse data
					var nameData = await planckeApiRequester
						.SendRequests();
					var checker = new ResponseParser(nameData);

					foreach (var data in checker.GetPlayerDataFromMap())
					{
						CachedPlayerData.TryAdd(data.Name, data);
						teamStats.Add(data);
					}

					nickedPlayers.AddRange(checker.ErroredPlayers);
				}

				teamInfo.Add(
					new TeamInfoResults(key, teamStats, nickedPlayers)
				);
			}

			teamInfo = teamInfo
				.OrderByDescending(TeamSortBySpecifiedType())
				.ToList();

			reqTime.Stop();
			var apiRequestTime = reqTime.Elapsed;

			// start parsing the data
			var rank = 1;

			var table = new Table(9);
			table.AddRow("Rank", "LVL", "Username", "Finals", "Beds", "FKDR", "WS", "Score", "Assessment")
				.AddSeparator();
			for (var i = 0; i < teamInfo.Count; i++)
			{
				var result = teamInfo[i];
				var ansiColorToUse = result.Color switch
				{
					"Blue" => TextBrightBlueAnsi,
					"Yellow" => TextYellowAnsi,
					"Green" => TextGreenAnsi,
					"Red" => TextRedAnsi,
					"Aqua" => TextCyanAnsi,
					"Grey" => TextBrightBlackAnsi,
					"Pink" => TextBrightRedAnsi,
					"White" => TextWhiteAnsi,
					_ => ResetAnsi
				};

				var allAvailablePlayers = result.AvailablePlayers
					.OrderByDescending(SortBySpecifiedType())
					.ToArray();

				var totalFinals = result.AvailablePlayers.Sum(x => x.FinalKills);
				var totalDeaths = result.AvailablePlayers.Sum(x => x.FinalDeaths);
				var totalLevel = result.AvailablePlayers
					.Where(x => x.Level != -1)
					.Sum(x => x.Level);
				table.AddRow(
					rank,
					totalLevel,
					$"{ansiColorToUse}[{result.Color} Team]{ResetAnsi}",
					result.AvailablePlayers.Sum(x => x.FinalKills),
					result.AvailablePlayers.Sum(x => x.BrokenBeds),
					totalDeaths == 0
						? "N/A"
						: Math.Round((double) totalFinals / totalDeaths, 2).ToString(CultureInfo.InvariantCulture),
					string.Empty,
					Math.Round(result.Score, 2),
					DetermineScoreMeaning(result.Score, true)
				);
				table.AddSeparator();

				foreach (var teammate in allAvailablePlayers)
				{
					table.AddRow(
						string.Empty,
						teammate.Level == -1 ? "N/A" : teammate.Level.ToString(),
						ansiColorToUse + (Config.DangerousPlayers.Contains(teammate.Name.ToLower())
							? $"(!) {teammate.Name}"
							: teammate.Name) + ResetAnsi,
						teammate.FinalKills,
						teammate.BrokenBeds,
						teammate.FinalDeaths == 0
							? "N/A"
							: Math.Round((double) teammate.FinalKills / teammate.FinalDeaths, 2)
								.ToString(CultureInfo.InvariantCulture),
						teammate.Winstreak == -1
							? "N/A"
							: teammate.Winstreak.ToString(),
						Math.Round(teammate.Score, 2),
						DetermineScoreMeaning(teammate.Score, true)
					);
				}

				foreach (var erroredPlayers in result.ErroredPlayers)
				{
					table.AddRow(
						string.Empty,
						"N/A",
						ansiColorToUse + erroredPlayers + ResetAnsi,
						string.Empty,
						string.Empty,
						string.Empty,
						string.Empty,
						string.Empty,
						BackgroundRedAnsi + "Nicked!" + ResetAnsi
					);
				}

				if (i + 1 != teamInfo.Count)
					table.AddSeparator();
				++rank;
			}

			Console.WriteLine(table.ToString());
			Console.WriteLine($"[INFO] Image Processing Time: {timeTaken.TotalSeconds} Sec.");
			Console.WriteLine($"[INFO] API Requests Time: {apiRequestTime.TotalSeconds} Sec.");
			Console.WriteLine(Divider);
		}

		private static string DetermineScoreMeaning(double score, bool isPlayer)
		{
			if (score <= 20) 
				return TextGreenAnsi + (isPlayer ? "Bad" : "Safe") + ResetAnsi;
			if (score > 20 && score <= 40)
				return TextBrightGreenAnsi + (isPlayer ? "Decent" : "Pretty Safe") + ResetAnsi;
			if (score > 40 && score <= 60)
				return TextBrightYellowAnsi + (isPlayer ? "Good" : "Somewhat Safe") + ResetAnsi;
			if (score > 60 && score <= 80) 
				return TextYellowAnsi + (isPlayer ? "Professional" : "Not Safe") + ResetAnsi;
			return TextRedAnsi + (isPlayer ? "Tryhard" : "Leave Now") + ResetAnsi;
		}

		public static string GamemodeIntToStr()
			=> Mode switch
			{
				12 => "Solos/Doubles",
				34 => "3v3v3v3s/4v4v4v4s/4v4s",
				_ => throw new ArgumentOutOfRangeException(nameof(Mode), "Gamemode must either be 34 or 12.")
			};

		internal static Func<BedwarsData, double> SortBySpecifiedType()
			=> SortingType switch
			{
				SortType.Beds => data => data.BrokenBeds,
				SortType.Finals => data => data.FinalKills,
				SortType.Fkdr => data =>
					data.FinalDeaths == 0 ? data.FinalKills : data.FinalKills / (double) data.FinalDeaths,
				SortType.Score => data => data.Score,
				SortType.Winstreak => data => data.Winstreak,
				SortType.Level => data => data.Level,
				_ => throw new ArgumentOutOfRangeException()
			};

		internal static Func<TeamInfoResults, double> TeamSortBySpecifiedType()
			=> SortingType switch
			{
				SortType.Beds => data => data.AvailablePlayers.Sum(x => x.BrokenBeds),
				SortType.Finals => data => data.AvailablePlayers.Sum(x => x.FinalKills),
				SortType.Fkdr => data =>
				{
					var fd = data.AvailablePlayers.Sum(x => x.FinalDeaths);
					var fk = data.AvailablePlayers.Sum(x => x.FinalKills);
					return fd == 0 ? fk : fk / (double) fd; 
				},
				SortType.Score => data => data.Score,
				SortType.Winstreak => data => data.AvailablePlayers.Sum(x => x.Winstreak),
				SortType.Level => data => data.AvailablePlayers.Sum(x => x.Level),
				_ => throw new ArgumentOutOfRangeException()
			};
	}

	public enum SortType
	{
		Winstreak,
		Score,
		Beds,
		Finals,
		Fkdr,
		Level
	}
}