using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Winstreak.Imaging;
using Winstreak.MethodExtensions;
using Winstreak.Parser;
using Winstreak.Parser.V1;
using Winstreak.Request;
using Winstreak.Request.Checker;

namespace Winstreak.Directory
{
	public class DirectoryWatcher
	{
		public static int FinalKills;
		public static int BrokenBeds;
		public static int MaxTryHards;
		public static string MCPath;
		public static int GuiScale; 

		public static void Run(string path, int finalKills, int brokenBeds, int maxTryhards)
		{
			FinalKills = finalKills;
			BrokenBeds = brokenBeds;
			MaxTryHards = maxTryhards;
			MCPath = path;

			GuiScale = ParserHelper.GetGuiScale(path);

			Console.WriteLine($"[INFO] Determined Width: {(GuiScale == 0 ? "AUTO" : GuiScale.ToString())}");
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

			while (Console.ReadLine() != "q")
			{
			}
		}

		private static void OnChanged(object source, FileSystemEventArgs e)
		{
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
				parser.CropImageIfFullScreen();
				parser.AdjustColors();
				parser.CropHeaderAndFooter();
				parser.FixImage();

				if (GuiScale == 0 || GuiScale == -1)
				{
					parser.IdentifyWidth();
					GuiScale = parser.Width;
				}
				else
				{
					parser.SetGuiScale(GuiScale);
				}
			}
			catch (Exception)
			{
				Console.WriteLine("[ERROR] An error occurred when trying to parse the image.");
				return;
			}

			IList<string> allNames = parser.GetPlayerName();
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

			Console.WriteLine($"[INFO] Errored: {checker.ErroredPlayers.Count} {checker.ErroredPlayers.ToReadableString()}");
			Console.WriteLine($"[INFO] Tryhards: {namesToWorryAbout.Count}");
			Console.WriteLine($"[INFO] Total: {allNames.Count}");
			Console.WriteLine($"[INFO] All Names: {allNames.Count}");
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
				{
					points += (int) ((percentFinalKillsByTryhards * 6) * finalKillsMultiplier);
				}
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
				parser.CropImageIfFullScreen();
				parser.AdjustColors();
				parser.CropHeaderAndFooter();
				parser.FixImage();

				if (GuiScale == 0 || GuiScale == -1)
				{
					parser.IdentifyWidth();
					GuiScale = parser.Width;
				}
				else
				{
					parser.SetGuiScale(GuiScale);
				}
			}
			catch (Exception)
			{
				Console.WriteLine("[ERROR] An error occurred when trying to parse the image.");
				return;
			}

			IDictionary<TeamColors, IList<string>> teams = parser.GetPlayerName();

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
					.Append($"Total Final Kills: {result.TotalFinalKills}")
					.AppendLine()
					.Append($"Total Broken Beds: {result.TotalBrokenBeds}")
					.AppendLine()
					.Append($"Players: {allAvailablePlayers}")
					.AppendLine()
					.Append($"Errored: {result.ErroredPlayers.ToReadableString()}")
					.AppendLine();

				Console.WriteLine(b.ToString());
				Console.WriteLine();
				++rank; 
			}

			processingTime.Stop();
			TimeSpan processedTime = processingTime.Elapsed;

			Console.WriteLine($"[INFO] Image Processing Time: {imageProcessingTime.TotalSeconds} Sec.");
			Console.WriteLine($"[INFO] API Requests Time: {apiRequestTime.TotalSeconds} Sec.");
			Console.WriteLine($"[INFO] Processing Reqeusts Time: {processedTime.TotalSeconds} Sec.");
			Console.WriteLine("=====================================");
		}
	}
}