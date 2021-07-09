using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Winstreak.Cli.Utility;
using Winstreak.Cli.Utility.ConsoleTable;
using Winstreak.Core.Extensions;
using Winstreak.Core.Profile.Calculations;
using static Winstreak.Core.WebApi.CachedData;
using Winstreak.Core.WebApi.Hypixel;
using Winstreak.Core.WebApi.Plancke;

namespace Winstreak.Cli.WSMain
{
	public static partial class WinstreakProgram
	{
		/// <summary>
		/// A function that is executed in a loop. Handles commands.
		/// </summary>
		/// <returns>Whether to terminate the program (q command).</returns>
		private static async Task<bool> RunCommandLoop()
		{
			var input = (Console.ReadLine() ?? string.Empty).Trim();
			if (input == string.Empty)
				return true;

			if (input.StartsWith('-'))
			{
				// quit program
				if (input.ToLower() == "-q" || input.ToLower() == "-quit")
					return false;

				var arguments = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
				switch (arguments[0].ToLower().Trim())
				{
					case "-config":
						var configTable = new Table(2)
							.AddRow("Name", "Value")
							.AddSeparator()
							.AddRow("MC Folder", Config.PathToMinecraftFolder)
							.AddRow("Logs Folder", Config.PathToLogsFolder)
							.AddRow("API Key Valid?", ApiKeyValid)
							.AddRow("Delete Screenshots?", Config.DeleteScreenshot)
							.AddRow("Checking Friends?", ApiKeyValid && Config.CheckFriends)
							.AddRow("Suppress Errors?", Config.SuppressErrorMessages)
							.AddRow("Screenshot Delay?", Config.SuppressErrorMessages)
							.AddRow("GUI Scale", GuiScale)
							.AddRow("Strict Parser?", Config.StrictParser);
						Console.WriteLine(configTable.ToString());
						Console.WriteLine($"Exempt Players: {Config.ExemptPlayers.ToReadableString()}");
						Console.WriteLine(Divider);
						return true;
					case "-help":
					case "-h":
						OutputDisplayer.WriteLine(LogType.Info, HelpInfo);
						Console.WriteLine(Divider);
						return true;
					case "-clear":
					case "-c":
						Console.Clear();
						return true;
					case "-tc":
						ShouldClearBeforeCheck = !ShouldClearBeforeCheck;
						OutputDisplayer.WriteLine(LogType.Info, ShouldClearBeforeCheck
							? "Console will be cleared once a screenshot is provided."
							: "Console will not be cleared once a screenshot is provided.");
						Console.WriteLine(Divider);
						return true;
					case "-status":
						var valid = HypixelApi != null && ApiKeyValid;
						var usage = valid
							? $"{HypixelApi.RequestsMade}/{HypixelApi.MaximumRequestsInRateLimit}"
							: "Unlimited (Plancke)";
						var statusTable = new Table(2)
							.AddRow("Name", "Value")
							.AddSeparator()
							.AddRow("API Key Valid?", valid)
							.AddRow("Usage", usage)
							.AddRow("Player Cache Count", CachedPlayerData.Length)
							.AddRow("Friend Cache Count", CachedFriendsData.Length)
							.AddRow("Sort Mode", SortingType);
						Console.WriteLine(statusTable.ToString());
						Console.WriteLine(Divider);
						return true;
					case "-clearcache":
					case "-emptycache":
						OutputDisplayer.WriteLine(LogType.Info, "Cache has been cleared.");
						CachedPlayerData.Empty();
						CachedFriendsData.Empty();
						CachedGuildData.Empty();
						Console.WriteLine(Divider);
						return true;
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

						OutputDisplayer.WriteLine(LogType.Info, $"Sorting By: {SortingType}");
						Console.WriteLine(Divider);
						return true;
					case "-party":
						OutputDisplayer.WriteLine(LogType.Info, $"{PartySession.Count} Party Members");
						foreach (var (lowercase, member) in PartySession)
							Console.WriteLine($"\t- {member} ({lowercase})");
						Console.WriteLine(Divider);
						return true;
					case "-stats":
					case "-statistics":
						var curTimeFormatted = DateTime.Now.ToString("HH:mm:ss");
						var purchasedMade = ItemStatistics.Values.Sum();
						var basicStats = new StringBuilder()
							.Append($"Winstreak.NET started at {StartedInstance:HH:mm:ss}.")
							.AppendLine()
							.Append($"As of {curTimeFormatted}, you have made {purchasedMade} purchases.")
							.AppendLine();
						foreach (var (item, qty) in ItemStatistics)
							basicStats.Append($"- {qty}x {item}").AppendLine();

						Console.WriteLine(basicStats.ToString());
						// Save 
						if (arguments.Length > 1 && (arguments[1] == "d" || arguments[1] == "s"))
						{
							var dir = Directory.Exists(AppContext.BaseDirectory)
								? new DirectoryInfo(AppContext.BaseDirectory)
								: default;
							if (Config.FileData is {Directory: { }})
								dir = Config.FileData.Directory;
							if (dir is null)
								OutputDisplayer.WriteLine(LogType.Error, "Couldn't find a folder to save to.");
							else
							{
								var time = $"{DateTime.Now.Hour}{DateTime.Now.Minute}{DateTime.Now.Second}";
								var fileName = $"bw_shop_stats_{time}.txt";
								var path = Path.Join(dir.FullName, fileName);
								await File.WriteAllTextAsync(path, basicStats.ToString());
								OutputDisplayer.WriteLine(LogType.Info, $"Saved stats to: {path}");
							}
						}

						Console.WriteLine(Divider);
						return true;

					case "-voiddeaths":
					case "-void":
						var inPartyDict = VoidDeaths
							.Where(x => PartySession.ContainsKey(x.Key.ToLower()))
							.ToDictionary(k => k.Key, v => v.Value);
						var notInPartyDict = VoidDeaths
							.Where(x => !PartySession.ContainsKey(x.Key.ToLower()))
							.ToDictionary(k => k.Key, v => v.Value);

						if (inPartyDict.Count == 0 && notInPartyDict.Count == 0)
							Console.WriteLine("No deaths by falling into void recorded.");
						else
						{
							if (inPartyDict.Count > 0)
							{
								var inPartyTable = new Table(2)
									.AddRow("Name (P)", "Void")
									.AddSeparator();
								foreach (var (name, count) in inPartyDict)
									inPartyTable.AddRow(name, count);
								Console.WriteLine(inPartyTable);
							}

							if (notInPartyDict.Count > 0)
							{
								var notInParty = new Table(2)
									.AddRow("Name (NP)", "Void")
									.AddSeparator();
								foreach (var (name, count) in notInPartyDict)
									notInParty.AddRow(name, count);
								Console.WriteLine(notInParty);
							}
						}

						Console.WriteLine(Divider);
						return true;
				}

				OutputDisplayer.WriteLine(LogType.Info, HelpInfo);
				Console.WriteLine(Divider);
				return true;
			}

			if (input.Contains('-') || input.Contains('\\'))
				return true;

			var ignsToCheck = input.Split(" ")
				.Select(x => x.Trim())
				.Where(x => x != string.Empty)
				.ToList();

			// check ign
			var checkTime = new Stopwatch();
			checkTime.Start();
			var (profiles, nicked) = await PlanckeApi
				.GetMultipleProfilesFromPlanckeAsync(ignsToCheck);

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

				var score = PlayerCalculator.GetScore(overallFkdr, overall.FinalKills,
					overall.BrokenBeds, overall.Level);
				Console.WriteLine($"> WS Score: {Math.Round(score, 3)}");
				Console.WriteLine($"> WS Classification: {DetermineScoreMeaning(score, true)}");
				Console.WriteLine($"> First Joined: {profiles[0].FirstJoined:MM/dd/yyyy hh:mm tt}");
			}
			else
			{
				var table = new Table(8)
					.AddRow("LVL", "Username", "FKDR", "Beds", "W/L", "WS", "Score", "Classification")
					.AddSeparator();
				foreach (var bedwarsData in profiles)
				{
					var score = PlayerCalculator.GetScore(bedwarsData.OverallBedwarsStats.GetFkdr(),
						bedwarsData.OverallBedwarsStats.FinalKills, bedwarsData.OverallBedwarsStats.BrokenBeds,
						bedwarsData.OverallBedwarsStats.Level);
					table.AddRow(
						bedwarsData.BedwarsLevel,
						bedwarsData.Name,
						bedwarsData.OverallBedwarsStats.FinalDeaths == 0
							? "N/A"
							: Math.Round(
									(double) bedwarsData.OverallBedwarsStats.FinalKills /
									bedwarsData.OverallBedwarsStats.FinalDeaths, 2)
								.ToString(CultureInfo.InvariantCulture),
						bedwarsData.OverallBedwarsStats.BrokenBeds,
						bedwarsData.OverallBedwarsStats.Losses == 0
							? "N/A"
							: Math.Round(
									(double) bedwarsData.OverallBedwarsStats.Wins /
									bedwarsData.OverallBedwarsStats.Losses, 2)
								.ToString(CultureInfo.InvariantCulture),
						bedwarsData.Winstreak,
						Math.Round(score, 3),
						DetermineScoreMeaning(score, true)
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
			return true;
		}
	}
}