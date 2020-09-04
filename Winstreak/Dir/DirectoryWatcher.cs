using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Winstreak.Extensions;
using Winstreak.External.Imaging;
using Winstreak.Parser;
using Winstreak.Parser.V1;
using Winstreak.Request;
using Winstreak.Request.Checker;
using Winstreak.Request.Definition;

namespace Winstreak.Dir
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

		public static int FinalKills;
		public static int BrokenBeds;
		public static string McPath;
		public static DirectoryInfo McScreenshotsPath;
		public static int GuiScale;
		public static string[] ExemptPlayers;
		public static bool ShouldClearBeforeCheck;

		public static async Task Run(string path)
		{
			McPath = path;
			McScreenshotsPath = new DirectoryInfo(Path.Join(McPath, "screenshots"));

			// get current directory
			var assemblyDirectory = Environment.CurrentDirectory;
			try
			{
				var realPath = Path.Join(assemblyDirectory, "Exempt.txt");
				ExemptPlayers = File.Exists(realPath)
					? await File.ReadAllLinesAsync(realPath)
					: new string[0];
			}
			finally
			{
				Console.WriteLine($"[INFO] Exempt Players Set: {ExemptPlayers.ToReadableString()}");
			}

			// Get gui scale
			GuiScale = ParserHelper.GetGuiScale(path);

			if (GuiScale == 0)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(
					"[ERROR] Please set a non-automatic GUI scale in your Minecraft settings and then restart the program.");
				Console.ResetColor();
				return;
			}

			Console.WriteLine($"[INFO] Using Gui Scale: {GuiScale}");
			Console.WriteLine("=========================");

			using var watcher = new FileSystemWatcher
			{
				Path = Path.Join(path, "screenshots"),
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
					Console.WriteLine($"> Regular K/D Ratio: {(double) playerInfo.Kills / playerInfo.Deaths}");
					Console.WriteLine($"> Final K/D Ratio: {(double) playerInfo.FinalKills / playerInfo.FinalDeaths}");
					Console.WriteLine($"> W/L Ratio: {(double) playerInfo.Wins / playerInfo.Losses}");
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
			await Task.Delay(350);
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

			var allNames = parser.GetPlayerName(ExemptPlayers).lobby;
			parser.Dispose();
			processingTime.Stop();
			var imageProcessingTime = processingTime.Elapsed;
			processingTime.Reset();

			processingTime.Start();
			// get data
			var planckeApiRequester = new PlanckeApiRequester(allNames);
			// parse data
			var nameData = await planckeApiRequester.SendRequests();
			var checker = new ResponseParser(nameData)
				.SetMinimumBrokenBedsNeeded(BrokenBeds)
				.SetMinimumFinalKillsNeeded(FinalKills);

			var nameResults = checker.GetPlayerDataFromMap();

			processingTime.Stop();
			var apiRequestTime = processingTime.Elapsed;
			processingTime.Reset();

			// start parsing the data
			// TODO maybe better way to format table? 
			var tableBuilder = new StringBuilder("┌────────────────────┬────────┬────────┬────────┬──────────┐")
				.AppendLine()
				.Append($"│{"Player Name",-20}│{"F. Kills",-8}│{"Beds",-8}│{"Score",-8}│{"Meaning",-10}│")
				.AppendLine()
				.Append("├────────────────────┼────────┼────────┼────────┼──────────┤")
				.AppendLine();
			foreach (var playerInfo in nameResults)
				tableBuilder
					.Append(
						$"│{playerInfo.Name,-20}│{playerInfo.FinalKills,-8}│{playerInfo.BedsBroken,-8}│{Math.Round(playerInfo.Score, 3),-8}│{DetermineScoreMeaning(playerInfo.Score), -10}│")
					.AppendLine();
			tableBuilder
				.Append("└────────────────────┴────────┴────────┴────────┴──────────┘");
			Console.WriteLine(tableBuilder.ToString());
			Console.WriteLine(
				$"[INFO] Errored: {checker.ErroredPlayers.Count} {checker.ErroredPlayers.ToReadableString()}");

			Console.WriteLine($"[INFO] Total Final Kills: {checker.TotalFinalKills}");
			Console.WriteLine($"[INFO] Total Broken Beds: {checker.TotalBedsBroken}");
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
					new TeamInfoResults(key, p.GetPlayerDataFromMap(), p.ErroredPlayers,
						p.TotalFinalKills, p.TotalBedsBroken)
				);
			}

			teamInfo = teamInfo
				.OrderByDescending(x => x.TotalBrokenBeds)
				.ToList();

			processingTime.Stop();
			var apiRequestTime = processingTime.Elapsed;
			processingTime.Reset();

			// start parsing results for data
			processingTime.Start();
			// start parsing the data
			var rank = 1;

			foreach (var result in teamInfo)
			{
				var allAvailablePlayers = result.AvailablePlayers
					.OrderByDescending(x => x.Score)
					.Select(x => $"{x.Name} ({Math.Round(x.Score, 1)})")
					.ToList()
					.ToReadableString();

				var b = new StringBuilder()
					.Append($"[{rank}] {result.Color} ({result.AvailablePlayers.Count + result.ErroredPlayers.Count})")
					.AppendLine()
					.Append($"{"",4}Total Final Kills: {result.TotalFinalKills}")
					.AppendLine()
					.Append($"{"",4}Total Broken Beds: {result.TotalBrokenBeds}")
					.AppendLine()
					.Append($"{"",4}Players: {allAvailablePlayers}")
					.AppendLine()
					.Append($"{"",4}Errored: {result.ErroredPlayers.ToReadableString()}")
					.AppendLine();

				Console.WriteLine(b.ToString());
				++rank;
			}

			Console.WriteLine($"[INFO] {string.Join(" ← ", teamInfo.Select(x => x.Color))}");

			processingTime.Stop();
			var processedTime = processingTime.Elapsed;

			Console.WriteLine($"[INFO] Image Processing Time: {imageProcessingTime.TotalSeconds} Sec.");
			Console.WriteLine($"[INFO] API Requests Time: {apiRequestTime.TotalSeconds} Sec.");
			Console.WriteLine($"[INFO] Processing Reqeusts Time: {processedTime.TotalSeconds} Sec.");
			Console.WriteLine("=====================================");
		}

		private static string DetermineScoreMeaning(double score)
		{
			if (score <= 20) return "Bad";
			if (score > 20 && score <= 40) return "Decent";
			if (score > 40 && score <= 60) return "Good";
			if (score > 60 && score <= 80) return "Pro";
			return "Tryhard";
		}
	}
}