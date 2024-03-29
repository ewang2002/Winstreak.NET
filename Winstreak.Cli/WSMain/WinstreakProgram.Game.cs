﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Winstreak.Cli.Utility;
using Winstreak.Cli.Utility.ConsoleTable;
using Winstreak.Core.Parsers.ImageParser;
using Winstreak.Core.Profile;
using Winstreak.Core.WebApi.Plancke;
using static Winstreak.Cli.Utility.ConsoleTable.AnsiConstants;

namespace Winstreak.Cli.WSMain
{
	public static partial class WinstreakProgram
	{
		/// <summary>
		/// Checks all teams in a game to see which team may potentially be dangerous. 
		/// </summary>
		/// <param name="teams">The teams.</param>
		/// <param name="timeTaken">The time taken for the screenshot to actually be processed.</param>
		/// <returns>Nothing.</returns>
		private static async Task ProcessInGameScreenshotAsync(IDictionary<TeamColor, IList<string>> teams,
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
							.GetMultipleProfilesFromPlanckeAsync(unableToSearch);
						
						teamStats.AddRange(profilePlancke);
						people.AddRange(profilePlancke);
						nickedPlayers.AddRange(nickedPlancke);
					}
				}
				else
				{
					var (profilePlancke, nickedPlancke) = await PlanckeApi
						.GetMultipleProfilesFromPlanckeAsync(value);

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

			var totalPeople = teamInfo.Sum(x => x.PlayersInTeam.Count + x.NickedPlayers.Count);
			var table = new Table(10);
			table.AddRow("Rank", "LVL", $"{totalPeople} Players", "Finals", "Beds", "FKDR", "WS", "Score", "Assessment",
					"G")
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

				var totalFinals = result.PlayersInTeam.Sum(x => x.OverallBedwarsStats.FinalKills);
				var totalDeaths = result.PlayersInTeam.Sum(x => x.OverallBedwarsStats.FinalDeaths);
				var totalLevel = result.PlayersInTeam
					.Where(x => x.BedwarsLevel != -1)
					.Sum(x => x.BedwarsLevel);
				table.AddRow(
					rank,
					totalLevel,
					$"{ansiColorToUse}[{result.TeamColor} Team]{ResetAnsi}",
					result.PlayersInTeam.Sum(x => x.OverallBedwarsStats.FinalKills),
					result.PlayersInTeam.Sum(x => x.OverallBedwarsStats.BrokenBeds),
					totalDeaths == 0
						? "N/A"
						: Math.Round((double)totalFinals / totalDeaths, 2).ToString(CultureInfo.InvariantCulture),
					string.Empty,
					Math.Round(result.CalculateScore(), 2),
					DetermineScoreMeaning(result.CalculateScore(), true),
					string.Empty
				);
				table.AddSeparator();

				foreach (var teammate in allAvailablePlayers)
				{
					var groupNum = GetGroupIndex(friendGroups, friendErrored, teammate.Name);
					var fkdr = teammate.OverallBedwarsStats.GetFkdr();
					table.AddRow(
						string.Empty,
						teammate.BedwarsLevel == -1 ? "N/A" : teammate.BedwarsLevel.ToString(),
						ansiColorToUse + teammate.Name + ResetAnsi,
						teammate.OverallBedwarsStats.FinalKills,
						teammate.OverallBedwarsStats.BrokenBeds,
						fkdr.fdZero ? "N/A" : Math.Round(fkdr.fkdr, 2).ToString(CultureInfo.InvariantCulture),
						teammate.Winstreak == -1
							? "N/A"
							: teammate.Winstreak.ToString(),
						Math.Round(teammate.OverallBedwarsStats.GetScore(), 2),
						DetermineScoreMeaning(teammate.OverallBedwarsStats.GetScore(), true),
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
			OutputDisplayer.WriteLine(LogType.Info, $"Image Processing Time: {timeTaken.TotalMilliseconds} MS.");
			OutputDisplayer.WriteLine(LogType.Info, $"API Requests Time: {apiRequestTime.TotalSeconds} SEC.");
			Console.WriteLine(Divider);
		}
	}
}