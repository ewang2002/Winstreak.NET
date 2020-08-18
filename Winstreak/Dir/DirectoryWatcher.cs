using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Winstreak.Extensions;
using Winstreak.Imaging;
using Winstreak.Parser;
using Winstreak.Parser.V1;
using Winstreak.Request;
using Winstreak.Request.Checker;

namespace Winstreak.Dir
{
	public class DirectoryWatcher
	{
		public static int FinalKills;
		public static int BrokenBeds;
		public static int MaxTryHards;
		public static string McPath;
		public static DirectoryInfo McScreenshotsPath; 
		public static int GuiScale;
		public static string[] ExemptPlayers;

		public static async Task Run(string path, int finalKills, int brokenBeds, int maxTryhards)
		{
			FinalKills = finalKills;
			BrokenBeds = brokenBeds;
			MaxTryHards = maxTryhards;
			McPath = path;
			McScreenshotsPath = new DirectoryInfo(Path.Join(McPath, "screenshots"));

			// get current directory
			string assemblyDirectory = Environment.CurrentDirectory;
			try
			{
				string realPath = Path.Join(assemblyDirectory, "Exempt.txt");
				ExemptPlayers = File.Exists(realPath) ? File.ReadAllLines(realPath) : new string[0];
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
				Console.WriteLine("[ERROR] Please set a non-automatic GUI scale and then restart the program.");
				Console.ResetColor();
				return;
			}

			Console.WriteLine($"[INFO] Using Gui Scale: {GuiScale}");
			Console.WriteLine("=========================");

			using FileSystemWatcher watcher = new FileSystemWatcher
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

			// infinite loop
			while (true)
			{
				string input = Console.ReadLine() ?? string.Empty;
				if (input == string.Empty) 
					continue;
				
				// quit program
				if (input.ToLower() == "-q")
					break;

				if (input.ToLower() == "-c")
				{
					Console.Clear();
					continue;
				}

				// check last image again
				if (input.ToLower() == "-l" || input.ToLower() == "-g")
				{
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
				}

				// check ign
				Stopwatch checkTime = new Stopwatch();
				checkTime.Start();
				HttpResponseMessage results = await PlanckeApiRequester.Client
					.GetAsync($"https://plancke.io/hypixel/player/stats/{input}");
				string responseHtml = await results.Content.ReadAsStringAsync();
				ResponseData data = new ResponseData(input, responseHtml)
					.Parse();
				if (data.TotalDataInfo is { } playerInfo)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"[INFO] {input} Found!");
					Console.ResetColor();
					Console.WriteLine($"[INFO] Broken Beds: {playerInfo.BrokenBeds}");
					Console.WriteLine($"[INFO] Final Kills: {playerInfo.FinalKills}");
					Console.WriteLine($"[INFO] Total Wins: {playerInfo.Wins}");
					Console.WriteLine($"[INFO] Total Losses: {playerInfo.Losses}");
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"[INFO] {input} Not Found!");
					Console.ResetColor();
				}
				checkTime.Stop();
				Console.WriteLine($"[INFO] Time Taken: {checkTime.Elapsed.TotalSeconds} Seconds.");
				Console.WriteLine("=====================================");
			}
		}

		private static void OnChanged(object source, FileSystemEventArgs e)
		{
			// wait for image to fully load
			Thread.Sleep(350);
			Bitmap bitmap = new Bitmap(ImageHelper.FromFile(e.FullPath));
			if (AbstractNameParser.IsInLobby(bitmap))
			{
#pragma warning disable 4014
				LobbyChecker(e.FullPath);
#pragma warning restore 4014
			}
			else
			{
#pragma warning disable 4014
				GameCheck(e.FullPath);
#pragma warning restore 4014
			}
		}

		private static async Task LobbyChecker(string bitmap)
		{
			Console.WriteLine($"[INFO] Checking Lobby: {bitmap}");
			Stopwatch processingTime = new Stopwatch();
			processingTime.Start();

			LobbyNameParser parser = new LobbyNameParser(bitmap);
			try
			{
				parser.SetGuiScale(GuiScale);
				parser.InitPoints();
				parser.FindStartOfName();
			}
			catch (Exception)
			{
				Console.WriteLine("[ERROR] An error occurred when trying to parse the image.");
				Console.WriteLine("=====================================");
				return;
			}

			IList<string> allNames = parser.GetPlayerName(ExemptPlayers).lobby;
			parser.Dispose();
			processingTime.Stop();
			TimeSpan imageProcessingTime = processingTime.Elapsed;
			processingTime.Reset();

			processingTime.Start();
			// get data
			PlanckeApiRequester planckeApiRequester = new PlanckeApiRequester(allNames);
			// parse data
			IDictionary<string, string> nameData = await planckeApiRequester.SendRequests();
			ResponseParser checker = new ResponseParser(nameData)
				.SetMinimumBrokenBedsNeeded(BrokenBeds)
				.SetMinimumFinalKillsNeeded(FinalKills);

			IList<ResponseCheckerResults> namesToWorryAbout = checker.GetNamesToWorryAbout();

			processingTime.Stop();
			TimeSpan apiRequestTime = processingTime.Elapsed;
			processingTime.Reset();

			// start parsing the data
			int tryhardBedsBroken = 0;
			int tryhardFinalKills = 0;
			if (namesToWorryAbout.Count != 0)
			{
				foreach (ResponseCheckerResults result in namesToWorryAbout)
				{
					tryhardBedsBroken += result.BedsBroken;
					tryhardFinalKills += result.FinalKills;
					Console.WriteLine(
						$"[PLAYER] Name: {result.Name} (K = {result.FinalKills} & B = {result.BedsBroken})");
				}
			}

			Console.WriteLine(
				$"[INFO] Errored: {checker.ErroredPlayers.Count} {checker.ErroredPlayers.ToReadableString()}");
			Console.WriteLine($"[INFO] Tryhards: {namesToWorryAbout.Count}");
			Console.WriteLine($"[INFO] Total: {allNames.Count}");
			Console.WriteLine($"[INFO] Tryhard Final Kills: {tryhardFinalKills}");
			Console.WriteLine($"[INFO] Tryhard Broken Beds: {tryhardBedsBroken}");
			Console.WriteLine($"[INFO] Total Final Kills: {checker.TotalFinalKills}");
			Console.WriteLine($"[INFO] Total Broken Beds: {checker.TotalBedsBroken}");
			Console.WriteLine($"[INFO] Image Processing Time: {imageProcessingTime.TotalSeconds} Sec.");
			Console.WriteLine($"[INFO] API Requests Time: {apiRequestTime.TotalSeconds} Sec.");

			int points = 0;
			if (namesToWorryAbout.Count >= MaxTryHards)
			{
				points += 20;
			}
			else
			{
				points += namesToWorryAbout.Count * 2;
			}

			points += checker.ErroredPlayers.Count;

			if (namesToWorryAbout.Count != 0)
			{
				int bedsThousands = tryhardBedsBroken / 1050;
				points += bedsThousands;

				int finalKillsThousands = tryhardFinalKills / 1350;
				points += finalKillsThousands;

				double percentBedsBrokenByTryhards = (double) tryhardBedsBroken / checker.TotalBedsBroken;
				int bedsBrokenMultiplier = tryhardBedsBroken >= BrokenBeds * namesToWorryAbout.Count
					? 1
					: 0;
				if (percentBedsBrokenByTryhards > 0.4)
				{
					points += (int) ((percentBedsBrokenByTryhards * 8) * bedsBrokenMultiplier);
				}

				double percentFinalKillsByTryhards = (double) tryhardFinalKills / checker.TotalFinalKills;
				double finalKillsMultiplier = tryhardFinalKills >= MaxTryHards * namesToWorryAbout.Count
					? 1
					: 0;
				if (percentFinalKillsByTryhards > 0.5)
					points += (int) (percentFinalKillsByTryhards * 6 * finalKillsMultiplier);
			}
			else
			{
				int bedsThousands = checker.TotalBedsBroken / 1050;
				points += bedsThousands;

				int finalKillsThousands = checker.TotalFinalKills / 1500;
				points += finalKillsThousands;
			}

			Console.WriteLine("[INFO] Points: " + points);
			// 16 to inf
			if (points >= 17)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[INFO] Suggested Action: LEAVE");
				Console.ResetColor();
			}
			else if (points >= 14)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[INFO] Suggested Action: SERIOUSLY CONSIDER LEAVING");
				Console.ResetColor();
			}
			else if (points >= 10)
			{
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine("[INFO] Suggested Action: CONSIDER LEAVING");
				Console.ResetColor();
			}
			else if (points >= 7)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("[INFO] Suggested Action: HARD GAME, CONSIDER STAYING");
				Console.ResetColor();
			}
			else if (points >= 4)
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine("[INFO] Suggested Action: NORMAL GAME, CONSIDER STAYING");
				Console.ResetColor();
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("[INFO] Suggested Action: SAFE TO STAY!");
				Console.ResetColor();
			}

			Console.WriteLine("=====================================");
		}

		public static async Task GameCheck(string bitmap)
		{
			Console.WriteLine($"[INFO] Checking Game: {bitmap}");
			Stopwatch processingTime = new Stopwatch();
			processingTime.Start();

			InGameNameParser parser = new InGameNameParser(bitmap);
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

			IDictionary<TeamColors, IList<string>> teams = parser.GetPlayerName().team;
			parser.Dispose();
			processingTime.Stop();
			TimeSpan imageProcessingTime = processingTime.Elapsed;
			processingTime.Reset();


			processingTime.Start();
			// get data
			IList<TeamInfoResults> teamInfo = new List<TeamInfoResults>();
			foreach (KeyValuePair<TeamColors, IList<string>> entry in teams)
			{
				PlanckeApiRequester planckeApiRequester = new PlanckeApiRequester(entry.Value);
				IDictionary<string, string> teamData = await planckeApiRequester.SendRequests();
				ResponseParser p = new ResponseParser(teamData);
				teamInfo.Add(new TeamInfoResults(entry.Key, p.GetPlayerDataFromMap(), p.ErroredPlayers,
					p.TotalFinalKills, p.TotalBedsBroken));
			}

			teamInfo = teamInfo
				.OrderByDescending(x => x.TotalBrokenBeds)
				.ToList();

			processingTime.Stop();
			TimeSpan apiRequestTime = processingTime.Elapsed;
			processingTime.Reset();

			// start parsing results for data
			processingTime.Start();
			// start parsing the data
			int rank = 1;

			foreach (TeamInfoResults result in teamInfo)
			{
				string allAvailablePlayers = result.AvailablePlayers
					.OrderByDescending(x => x.BedsBroken)
					.Select(x => $"{x.Name} ({x.BedsBroken})")
					.ToList()
					.ToReadableString();

				StringBuilder b = new StringBuilder()
					.Append($"[{rank}] {result.Color} ({result.AvailablePlayers.Count + result.ErroredPlayers.Count})")
					.AppendLine()
					.Append($"{"", 4}Total Final Kills: {result.TotalFinalKills}")
					.AppendLine()
					.Append($"{"", 4}Total Broken Beds: {result.TotalBrokenBeds}")
					.AppendLine()
					.Append($"{"", 4}Players: {allAvailablePlayers}")
					.AppendLine()
					.Append($"{"", 4}Errored: {result.ErroredPlayers.ToReadableString()}")
					.AppendLine();

				Console.WriteLine(b.ToString());
				++rank;
			}

			Console.WriteLine($"[INFO] {string.Join(" ← ", teamInfo.Select(x => x.Color))}");

			processingTime.Stop();
			TimeSpan processedTime = processingTime.Elapsed;

			Console.WriteLine($"[INFO] Image Processing Time: {imageProcessingTime.TotalSeconds} Sec.");
			Console.WriteLine($"[INFO] API Requests Time: {apiRequestTime.TotalSeconds} Sec.");
			Console.WriteLine($"[INFO] Processing Reqeusts Time: {processedTime.TotalSeconds} Sec.");
			Console.WriteLine("=====================================");
		}
	}
}