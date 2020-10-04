using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Winstreak.Parsers.ImageParser;
using Winstreak.Utility.ConsoleTable;
using Winstreak.WebApi.Definition;
using Winstreak.WebApi.Plancke;
using Winstreak.WebApi.Plancke.Checker;
using static Winstreak.Utility.ConsoleTable.AnsiConstants;

namespace Winstreak.DirectoryManager
{
	public static partial class DirectoryWatcher
	{
		/// <summary>
		/// Checks all teams in a game to see which team may potentially be dangerous. 
		/// </summary>
		/// <param name="teams">The teams.</param>
		/// <param name="timeTaken">The time taken for the screenshot to actually be processed.</param>
		/// <returns>Nothing.</returns>
		public static async Task ProcessInGameScreenshotAsync(IDictionary<TeamColor, IList<string>> teams,
			TimeSpan timeTaken)
		{
			var reqTime = new Stopwatch();
			reqTime.Start();
			// req data from plancke 
			var teamInfo = new List<TeamInfoResults>();
			var people = new List<BedwarsData>();

			foreach (var (key, value) in teams)
			{
				var teamStats = new List<BedwarsData>();
				var nickedPlayers = new List<string>();

				if (HypixelApi != null && ApiKeyValid)
				{
					var (responses, nicked, unableToSearch) = await HypixelApi.GetAllPlayersAsync(value);

					teamStats.AddRange(responses);
					people.AddRange(responses);
					nickedPlayers.AddRange(nicked);

					if (unableToSearch.Count != 0)
					{
						var planckeApiRequester = new PlanckeApiRequester(unableToSearch);
						var teamData = await planckeApiRequester
							.SendRequestsAsync();
						var p = new ResponseParser(teamData);
						teamStats.AddRange(p.GetPlayerDataFromMap());
						people.AddRange(p.GetPlayerDataFromMap());
						nickedPlayers.AddRange(p.ErroredPlayers);
					}
				}
				else
				{
					var planckeApiRequester = new PlanckeApiRequester(value);
					// parse data
					var nameData = await planckeApiRequester
						.SendRequestsAsync();
					var checker = new ResponseParser(nameData);

					teamStats.AddRange(checker.GetPlayerDataFromMap());
					people.AddRange(checker.GetPlayerDataFromMap());
					nickedPlayers.AddRange(checker.ErroredPlayers);
				}

				teamInfo.Add(
					new TeamInfoResults(key, teamStats, nickedPlayers)
				);
			}

			// get all friends
			var friendGroups = new List<IList<BedwarsData>>();
			var friendErrored = new List<string>();

			if (HypixelApi != null && ApiKeyValid)
			{
				var (friendG, friendU) = await GetGroups(people);
				friendGroups.AddRange(friendG);
				friendErrored.AddRange(friendU);
			}

			teamInfo = teamInfo
				.OrderByDescending(TeamSortBySpecifiedType())
				.ToList();

			reqTime.Stop();
			var apiRequestTime = reqTime.Elapsed;

			// start parsing the data
			var rank = 1;

			var table = new Table(10);
			table.AddRow("Rank", "LVL", "Username", "Finals", "Beds", "FKDR", "WS", "Score", "Assessment", "P")
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
					DetermineScoreMeaning(result.Score, true),
					string.Empty
				);
				table.AddSeparator();

				foreach (var teammate in allAvailablePlayers)
				{
					var groupNum = GetGroupIndex(friendGroups, friendErrored, teammate.Name);
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
						DetermineScoreMeaning(teammate.Score, true),
						groupNum == -2
							? "E"
							: groupNum == -1
								? string.Empty
								: groupNum.ToString()
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
						BackgroundRedAnsi + "Nicked!" + ResetAnsi,
						"E"
					);
				}

				if (i + 1 != teamInfo.Count)
					table.AddSeparator();
				++rank;
			}

			Console.WriteLine(table.ToString());
			Console.WriteLine($"[INFO] Image Processing Time: {timeTaken.TotalMilliseconds} Milliseconds.");
			Console.WriteLine($"[INFO] API Requests Time: {apiRequestTime.TotalSeconds} Sec.");
			Console.WriteLine(Divider);
		}
	}
}