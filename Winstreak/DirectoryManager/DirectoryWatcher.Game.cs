using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Winstreak.Parsers.ImageParser;
using Winstreak.Profile;
using Winstreak.Utility.ConsoleTable;
using Winstreak.WebApi.Plancke;
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
			var teamInfo = new List<TeamProfile>();
			var people = new List<PlayerProfile>();

			foreach (var (key, value) in teams)
			{
				var teamStats = new List<PlayerProfile>();
				var nickedPlayers = new List<string>();

				if (HypixelApi != null && ApiKeyValid)
				{
					var (responses, nicked, unableToSearch) = await HypixelApi.GetAllPlayersAsync(value);

					teamStats.AddRange(responses);
					people.AddRange(responses);
					nickedPlayers.AddRange(nicked);

					if (unableToSearch.Count != 0)
					{
						var (profilePlancke, nickedPlancke) = await PlanckeApi
							.GetMultipleProfilesFromPlancke(unableToSearch);
						
						teamStats.AddRange(profilePlancke);
						people.AddRange(profilePlancke);
						nickedPlayers.AddRange(nickedPlancke);
					}
				}
				else
				{
					var (profilePlancke, nickedPlancke) = await PlanckeApi
						.GetMultipleProfilesFromPlancke(value);

					teamStats.AddRange(profilePlancke);
					people.AddRange(profilePlancke);
					nickedPlayers.AddRange(nickedPlancke);
				}

				teamInfo.Add(
					new TeamProfile(key.ToString(), teamStats, nickedPlayers)
				);
			}

			// get all friends
			var friendGroups = new List<IList<PlayerProfile>>();
			var friendErrored = new List<string>();

			if (HypixelApi != null && ApiKeyValid && Config.CheckFriends)
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
			table.AddRow("Rank", "LVL", "Username", "Finals", "Beds", "FKDR", "WS", "Score", "Assessment", "G")
				.AddSeparator();
			for (var i = 0; i < teamInfo.Count; i++)
			{
				var result = teamInfo[i];
				var ansiColorToUse = result.TeamColor switch
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

				var allAvailablePlayers = result.PlayersInTeam
					.OrderByDescending(SortBySpecifiedType())
					.ToArray();

				var totalFinals = result.PlayersInTeam.Sum(x => x.BedwarsStats.FinalKills);
				var totalDeaths = result.PlayersInTeam.Sum(x => x.BedwarsStats.FinalDeaths);
				var totalLevel = result.PlayersInTeam
					.Where(x => x.BedwarsStats.BedwarsLevel != -1)
					.Sum(x => x.BedwarsStats.BedwarsLevel);
				table.AddRow(
					rank,
					totalLevel,
					$"{ansiColorToUse}[{result.TeamColor} Team]{ResetAnsi}",
					result.PlayersInTeam.Sum(x => x.BedwarsStats.FinalKills),
					result.PlayersInTeam.Sum(x => x.BedwarsStats.BrokenBeds),
					totalDeaths == 0
						? "N/A"
						: Math.Round((double) totalFinals / totalDeaths, 2).ToString(CultureInfo.InvariantCulture),
					string.Empty,
					Math.Round(result.CalculateScore(), 2),
					DetermineScoreMeaning(result.CalculateScore(), true),
					string.Empty
				);
				table.AddSeparator();

				foreach (var teammate in allAvailablePlayers)
				{
					var groupNum = GetGroupIndex(friendGroups, friendErrored, teammate.Name);
					var fkdr = teammate.BedwarsStats.GetFkdr();
					table.AddRow(
						string.Empty,
						teammate.BedwarsStats.BedwarsLevel == -1 ? "N/A" : teammate.BedwarsStats.BedwarsLevel.ToString(),
						ansiColorToUse + (Config.DangerousPlayers.Contains(teammate.Name.ToLower())
							? $"(!) {teammate.Name}"
							: teammate.Name) + ResetAnsi,
						teammate.BedwarsStats.FinalKills,
						teammate.BedwarsStats.BrokenBeds,
						fkdr.fdZero ? "N/A" : Math.Round(fkdr.fkdr, 2).ToString(CultureInfo.InvariantCulture),
						teammate.BedwarsStats.Winstreak == -1
							? "N/A"
							: teammate.BedwarsStats.Winstreak.ToString(),
						Math.Round(teammate.BedwarsStats.GetScore(), 2),
						teammate.BedwarsStats.FinalDeaths == 0
							? BackgroundBrightRedAnsi + "Poss. Alt./Sus." + ResetAnsi
							: DetermineScoreMeaning(teammate.BedwarsStats.GetScore(), true),
						groupNum switch
						{
							-2 => "E",
							-1 => string.Empty,
							_ => $"{groupNum}"
						}
					);
				}

				foreach (var erroredPlayers in result.NickedPlayers)
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