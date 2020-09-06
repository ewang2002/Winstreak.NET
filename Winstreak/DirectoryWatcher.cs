using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Winstreak.Calculations;
using Winstreak.ConfigParser;
using Winstreak.ConsoleTable;
using Winstreak.Extensions;
using Winstreak.External.Imaging;
using Winstreak.Parser;
using Winstreak.Request;
using Winstreak.Request.Checker;
using Winstreak.Request.Definition;
using static Winstreak.ConsoleTable.AnsiConstants;

namespace Winstreak
{
	public class DirectoryWatcher
	{
		public static string HelpInfo = new StringBuilder()
			.Append("[INFO] Current Command List.")
			.AppendLine()
			.Append("> -c: Clears the console.")
			.AppendLine()
			.Append("> -l: Checks the last screenshot provided, assuming it's a lobby screenshot.")
			.AppendLine()
			.Append("> -g: Checks the last screenshot provided, assuming it's an in-game screenshot.")
			.AppendLine()
			.Append(
				"> -tc: Determines whether the console should be cleared when a screenshot is provided.")
			.AppendLine()
			.Append("> -h: Shows this menu.")
			.ToString();

		public static DirectoryInfo McScreenshotsPath;
		public static int GuiScale;
		public static bool ShouldClearBeforeCheck;
		public static ConfigFile Config;

		public static async Task Run(ConfigFile file)
		{
			Config = file;
			McScreenshotsPath = new DirectoryInfo(Path.Join(Config.PathToMinecraftFolder, "screenshots"));
			ShouldClearBeforeCheck = file.ClearConsole;

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
			Console.WriteLine($"[INFO] Exempt Players Set: {Config.ExemptPlayers.ToReadableString()}");
			Console.WriteLine($"[INFO] Screenshot Delay Set: {Config.ScreenshotDelay} MS");
			Console.WriteLine($"[INFO] Retry Request Delay Set: {Config.RetryDelay} MS");
			Console.WriteLine($"[INFO] Retry Request Max Set: {Config.RetryMax}");
			Console.WriteLine($"[INFO] Using Gui Scale: {GuiScale}");
			Console.WriteLine("=========================");

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
				var input = Console.ReadLine() ?? string.Empty;
				if (input == string.Empty)
					continue;

				if (input.StartsWith('-'))
				{
					// quit program
					if (input.ToLower() == "-q")
						break;

					switch (input.ToLower().Trim())
					{
						case "-h":
							Console.WriteLine(HelpInfo);
							Console.WriteLine("=====================================");
							continue;
						case "-c":
							Console.Clear();
							continue;
						// check last image again
						case "-l":
						case "-g":
							if (McScreenshotsPath.GetFiles().Length == 0)
							{
								Console.WriteLine("[INFO] No Screenshots Found.");
								Console.WriteLine("=====================================");
								continue;
							}

							var lastFile = McScreenshotsPath
								.GetFiles()
								.OrderByDescending(x => x.LastWriteTime)
								.First();
							if (lastFile == null)
								continue;

							if (input.ToLower() == "-l")
								await LobbyChecker(lastFile.FullName);
							else
								await GameCheck(lastFile.FullName);
							continue;
						case "-tc":
							ShouldClearBeforeCheck = !ShouldClearBeforeCheck;
							Console.WriteLine(ShouldClearBeforeCheck
								? "[INFO] Console will be cleared once a screenshot is provided."
								: "[INFO] Console will not be cleared once a screenshot is provided.");
							Console.WriteLine("=====================================");
							continue;
					}

					Console.WriteLine(HelpInfo);
					Console.WriteLine("=====================================");
					continue;
				}

				// check ign
				var checkTime = new Stopwatch();
				checkTime.Start();
				var results = await PlanckeApiRequester.Client
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
					Console.WriteLine($"> Regular K/D Ratio: {(double)playerInfo.Kills / playerInfo.Deaths}");
					Console.WriteLine($"> Final K/D Ratio: {(double)playerInfo.FinalKills / playerInfo.FinalDeaths}");
					Console.WriteLine($"> W/L Ratio: {(double)playerInfo.Wins / playerInfo.Losses}");
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($@"[INFO] ""{input}"" Not Found!");
					Console.ResetColor();
				}

				checkTime.Stop();
				Console.WriteLine($"> Time Taken: {checkTime.Elapsed.TotalSeconds} Seconds.");
				Console.WriteLine("=====================================");
			}
		}

		private static async void OnChanged(object source, FileSystemEventArgs e)
		{
			// wait for image to fully load
			await Task.Delay(Config.ScreenshotDelay);
			var bitmap = new Bitmap(ImageHelper.FromFile(e.FullPath));
			if (AbstractNameParser.IsInLobby(bitmap))
				await LobbyChecker(e.FullPath);
			else
				await GameCheck(e.FullPath);
		}

		private static async Task LobbyChecker(string bitmap)
		{
			if (ShouldClearBeforeCheck)
				Console.Clear();
			Console.WriteLine($"[INFO] Checking Lobby: {bitmap}");
			var processingTime = new Stopwatch();
			processingTime.Start();

			var parser = new LobbyNameParser(bitmap);
			try
			{
				parser.SetGuiScale(GuiScale);
				parser.InitPoints();
				parser.FindStartOfName();
			}
			catch (Exception)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[ERROR] An error occurred when trying to parse the image.");
				Console.ResetColor();
				Console.WriteLine("=====================================");
				return;
			}

			var allNames = parser.GetPlayerName(Config.ExemptPlayers).lobby;
			parser.Dispose();
			processingTime.Stop();
			var imageProcessingTime = processingTime.Elapsed;
			processingTime.Reset();

			processingTime.Start();
			// get data
			var planckeApiRequester = new PlanckeApiRequester(allNames);
			// parse data
			var nameData = await planckeApiRequester.SendRequests();
			var checker = new ResponseParser(nameData);
			var nameResults = checker.GetPlayerDataFromMap();

			processingTime.Stop();
			var apiRequestTime = processingTime.Elapsed;
			processingTime.Reset();

			// start parsing the data
			var tableBuilder = new Table(6)
				.AddRow("Username", "Final Kills", "Broken Beds", "FKDR", "Score", "Assessment")
				.AddSeparator();
			foreach (var playerInfo in nameResults)
				tableBuilder.AddRow(
					playerInfo.Name,
					playerInfo.FinalKills,
					playerInfo.BrokenBeds,
					playerInfo.FinalDeaths == 0
						? "N/A"
						: Math.Round((double)playerInfo.FinalKills / playerInfo.FinalDeaths, 2)
							.ToString(CultureInfo.InvariantCulture), Math.Round(playerInfo.Score, 2),
					DetermineScoreMeaning(playerInfo.Score, true)
				);

			foreach (var erroredPlayer in checker.ErroredPlayers)
				tableBuilder.AddRow(
					BackgroundRedAnsi + erroredPlayer + ResetAnsi,
					BackgroundRedAnsi + "N/A" + ResetAnsi,
					BackgroundRedAnsi + "N/A" + ResetAnsi,
					BackgroundRedAnsi + "N/A" + ResetAnsi,
					BackgroundRedAnsi + "N/A" + ResetAnsi,
					BackgroundRedAnsi + "Nicked!" + ResetAnsi
				);

			tableBuilder.AddSeparator();
			var ttlScore = PlayerCalculator.CalculatePlayerThreatLevel(checker.TotalWins, checker.TotalLosses,
				checker.TotalFinalKills, checker.TotalFinalDeaths, checker.TotalBedsBroken);
			tableBuilder.AddRow(
				"Total",
				checker.TotalFinalKills,
				checker.TotalBedsBroken,
				checker.TotalLosses == 0
					? "N/A"
					: Math.Round((double)checker.TotalWins / checker.TotalLosses, 2)
						.ToString(CultureInfo.InvariantCulture),
				Math.Round(ttlScore, 2),
				DetermineScoreMeaning(ttlScore, false)
			);
			Console.WriteLine(tableBuilder.ToString());

			Console.WriteLine($"[INFO] Image Processing Time: {imageProcessingTime.TotalSeconds} Sec.");
			Console.WriteLine($"[INFO] API Requests Time: {apiRequestTime.TotalSeconds} Sec.");

			Console.WriteLine("=====================================");
		}

		public static async Task GameCheck(string bitmap)
		{
			if (ShouldClearBeforeCheck)
				Console.Clear();
			Console.WriteLine($"[INFO] Checking Game: {bitmap}");
			var processingTime = new Stopwatch();
			processingTime.Start();

			var parser = new InGameNameParser(bitmap);
			try
			{
				parser.SetGuiScale(GuiScale);
				parser.InitPoints();
				parser.FindStartOfName();
				parser.AccountForTeamLetters();
			}
			catch (Exception)
			{
				Console.WriteLine("[ERROR] An error occurred when trying to parse the image.");
				Console.WriteLine("=====================================");
				return;
			}

			var teams = parser.GetPlayerName().team;
			parser.Dispose();
			processingTime.Stop();
			var imageProcessingTime = processingTime.Elapsed;
			processingTime.Reset();

			processingTime.Start();
			// get data
			var teamInfo = new List<TeamInfoResults>();
			foreach (var (key, value) in teams)
			{
				var planckeApiRequester = new PlanckeApiRequester(value);
				var teamData = await planckeApiRequester.SendRequests();
				var p = new ResponseParser(teamData);
				teamInfo.Add(
					new TeamInfoResults(key, p.GetPlayerDataFromMap(), p.ErroredPlayers)
				);
			}

			teamInfo = teamInfo
				.OrderByDescending(x => x.Score)
				.ToList();

			processingTime.Stop();
			var apiRequestTime = processingTime.Elapsed;
			processingTime.Reset();

			// start parsing results for data
			processingTime.Start();
			// start parsing the data
			var rank = 1;

			var table = new Table(7);
			table.AddRow("Rank", "Username", "Finals", "Beds", "FKDR", "Score", "Assessment")
				.AddSeparator();
			for (var i = 0; i < teamInfo.Count; i++)
			{
				var result = teamInfo[i];
				var ansiColorToUse = result.Color == "Blue"
					? TextCyanAnsi
					: result.Color == "Yellow"
						? TextYellowAnsi
						: result.Color == "Green"
							? TextGreenAnsi
							: TextRedAnsi;

				var allAvailablePlayers = result.AvailablePlayers
					.OrderByDescending(x => x.Score)
					.ToArray();

				var totalFinals = result.AvailablePlayers.Sum(x => x.FinalKills);
				var totalDeaths = result.AvailablePlayers.Sum(x => x.FinalDeaths);
				table.AddRow(
					rank,
					$"{ansiColorToUse}[{result.Color} Team]{ResetAnsi}",
					result.AvailablePlayers.Sum(x => x.FinalKills),
					result.AvailablePlayers.Sum(x => x.BrokenBeds),
					totalDeaths == 0
						? "N/A"
						: Math.Round((double)totalFinals / totalDeaths, 2).ToString(CultureInfo.InvariantCulture),
					Math.Round(result.Score, 2),
					DetermineScoreMeaning(result.Score, true)
				);
				table.AddSeparator();

				foreach (var teammate in allAvailablePlayers)
				{
					table.AddRow(
						string.Empty,
						ansiColorToUse + teammate.Name + ResetAnsi,
						teammate.FinalKills,
						teammate.BrokenBeds,
						teammate.FinalDeaths == 0
							? "N/A"
							: Math.Round((double)teammate.FinalKills / teammate.FinalDeaths, 2)
								.ToString(CultureInfo.InvariantCulture),
						Math.Round(teammate.Score, 2),
						DetermineScoreMeaning(teammate.Score, true)
					);
				}

				foreach (var erroredPlayers in result.ErroredPlayers)
				{
					table.AddRow(
						string.Empty,
						ansiColorToUse + erroredPlayers + ResetAnsi,
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

			Console.WriteLine($"[INFO] {string.Join(" ← ", teamInfo.Select(x => x.Color))}");

			processingTime.Stop();
			var processedTime = processingTime.Elapsed;

			Console.WriteLine($"[INFO] Image Processing Time: {imageProcessingTime.TotalSeconds} Sec.");
			Console.WriteLine($"[INFO] API Requests Time: {apiRequestTime.TotalSeconds} Sec.");
			Console.WriteLine($"[INFO] Processing Reqeusts Time: {processedTime.TotalSeconds} Sec.");
			Console.WriteLine("=====================================");
		}

		private static string DetermineScoreMeaning(double score, bool isPlayer)
		{
			if (score <= 20) return TextGreenAnsi + (isPlayer ? "Bad" : "Safe") + ResetAnsi;
			if (score > 20 && score <= 40) return TextBrightGreenAnsi + (isPlayer ? "Decent" : "Pretty Safe") + ResetAnsi;
			if (score > 40 && score <= 60) return TextBrightYellowAnsi + (isPlayer ? "Good" : "Somewhat Safe") + ResetAnsi;
			if (score > 60 && score <= 80) return TextYellowAnsi + (isPlayer ? "Professional" : "Not Safe") + ResetAnsi;
			return TextRedAnsi + (isPlayer ? "Tryhard" : "Leave Now") + ResetAnsi;
		}
	}
}