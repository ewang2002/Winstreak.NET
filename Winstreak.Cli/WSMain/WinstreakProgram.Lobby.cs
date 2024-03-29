﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Winstreak.Cli.Utility;
using Winstreak.Cli.Utility.ConsoleTable;
using Winstreak.Core.Profile;
using Winstreak.Core.Profile.Calculations;
using Winstreak.Core.WebApi.Plancke;
using static Winstreak.Cli.Utility.ConsoleTable.AnsiConstants;
using static Winstreak.Core.WebApi.CachedData;

namespace Winstreak.Cli.WSMain
{
	public static partial class WinstreakProgram
	{
		/// <summary>
		/// Checks to see if a lobby is suitable enough for an "easy" game.
		/// </summary>
		/// <param name="names">The names to check.</param>
		/// <param name="timeTaken">The time taken for the screenshot to actually be processed.</param>
		/// <returns>Nothing.</returns>
		[SuppressMessage("Microsoft.Style", "IDE0042")]
		private static async Task ProcessLobbyScreenshotAsync(IList<string> names, TimeSpan timeTaken)
		{
			var reqTime = new Stopwatch();
			reqTime.Start();
			var nickedPlayers = new List<string>();
			var totalStats = new BedwarsStats();
			var levels = 0;

			var nameResults = new List<PlayerProfile>();

			// check hypixel api
			if (HypixelApi != null && ApiKeyValid)
			{
				var (responses, nicked, unableToSearch) = await HypixelApi
					.GetAllPlayersAsync(names.ToList());
				nickedPlayers = nicked.ToList();

				foreach (var resp in responses)
				{
					totalStats += resp.OverallBedwarsStats;
					levels += resp.BedwarsLevel;

					CachedPlayerData.TryAdd(resp.Name, resp);
					nameResults.Add(resp);
				}

				// request leftover data from plancke
				var (profilesPlancke, nickedPlancke) = await PlanckeApi
					.GetMultipleProfilesFromPlanckeAsync(unableToSearch);

				foreach (var playerInfo in profilesPlancke)
				{
					totalStats += playerInfo.OverallBedwarsStats;
					if (playerInfo.BedwarsLevel != -1)
						levels += playerInfo.BedwarsLevel;

					CachedPlayerData.TryAdd(playerInfo.Name, playerInfo);
					nameResults.Add(playerInfo);
				}

				nickedPlayers.AddRange(nickedPlancke);
			}
			else
			{
				// request data from plancke
				var (profilesPlancke, nickedPlancke) = await PlanckeApi
					.GetMultipleProfilesFromPlanckeAsync(names.ToList());

				foreach (var playerInfo in profilesPlancke)
				{
					totalStats += playerInfo.OverallBedwarsStats;
					if (playerInfo.BedwarsLevel != -1)
						levels += playerInfo.BedwarsLevel;

					CachedPlayerData.TryAdd(playerInfo.Name, playerInfo);
					nameResults.Add(playerInfo);
				}

				nickedPlayers.AddRange(nickedPlancke);
			}

			nameResults = nameResults
				.OrderByDescending(SortBySpecifiedType())
				.ToList();

			// start parsing the data
			var tableBuilder = new Table(8)
				.AddRow("LVL", $"{names.Count} Players", "Finals", "Beds", "FKDR", "WS", "Score", "Assessment")
				.AddSeparator();

			foreach (var playerInfo in nameResults)
			{
				var fkdr = playerInfo.OverallBedwarsStats.GetFkdr();
				var score = playerInfo.OverallBedwarsStats.GetScore();
				var playerName = playerInfo.Name;

				if ((DateTime.Now - playerInfo.FirstJoined).TotalDays <= 7)
					playerName = TextBrightBlackAnsi + playerName + ResetAnsi;

				tableBuilder.AddRow(
					playerInfo.BedwarsLevel == -1
						? "N/A"
						: playerInfo.BedwarsLevel.ToString(),
					playerName,
					playerInfo.OverallBedwarsStats.FinalKills,
					playerInfo.OverallBedwarsStats.BrokenBeds,
					fkdr.fdZero ? "N/A" : Math.Round(fkdr.fkdr, 2).ToString(CultureInfo.InvariantCulture),
					playerInfo.Winstreak == -1
						? "N/A"
						: playerInfo.Winstreak.ToString(),
					Math.Round(score, 1),
					DetermineScoreMeaning(score, true)
				);
			}

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
			var ttlScore = PlayerCalculator.GetScore(
				totalStats.GetFkdr(),
				totalStats.FinalKills,
				totalStats.BrokenBeds,
				levels
			);

			var totalWinLossRatio = totalStats.GetWinLossRatio();
			tableBuilder.AddRow(
				levels,
				"Total",
				totalStats.FinalKills,
				totalStats.BrokenBeds,
				totalWinLossRatio.lZero
					? "N/A"
					: Math.Round(totalWinLossRatio.wlr, 2) + "",
				string.Empty,
				Math.Round(ttlScore, 1),
				DetermineScoreMeaning(ttlScore, false)
			);

			// check friends
			Table friendTableBuilder = null;
			if (HypixelApi != null && ApiKeyValid && Config.CheckFriends)
			{
				var (friendGroups, nameFriendsUnable) = await GetGroups(nameResults);

				if (friendGroups.Count != 0)
				{
					friendTableBuilder = new Table(5)
						.AddRow("LVL", $"{friendGroups.Count} Friend Groups", "FKDR", "Score", "Assessment")
						.AddSeparator();

					for (var i = 0; i < friendGroups.Count; i++)
					{
						var friendGroup = friendGroups[i];
						foreach (var member in friendGroup)
						{
							var fkdr = member.OverallBedwarsStats.GetFkdr();
							friendTableBuilder.AddRow(
								member.BedwarsLevel,
								member.Name,
								fkdr.fdZero ? "N/A" : Math.Round(fkdr.fkdr, 2).ToString(CultureInfo.InvariantCulture),
								Math.Round(member.OverallBedwarsStats.GetScore(), 2),
								DetermineScoreMeaning(member.OverallBedwarsStats.GetScore(), true)
							);
						}

						if (i + 1 != friendGroups.Count)
							friendTableBuilder.AddSeparator();
					}

					friendTableBuilder
						.AddSeparator()
						.AddRow(string.Empty, $"{nameFriendsUnable.Count} Names Not Checked", string.Empty,
							string.Empty,
							string.Empty);
				}
				else
				{
					friendTableBuilder = new Table(1)
						.AddRow(BackgroundBrightGreenAnsi + "No Friend Groups Detected!" + ResetAnsi)
						.AddSeparator()
						.AddRow($"{nameFriendsUnable.Count} Names Not Checked.");
				}
			}

			reqTime.Stop();
			var apiRequestTime = reqTime.Elapsed;

			Console.WriteLine(tableBuilder.ToString());
			if (friendTableBuilder != null)
				Console.WriteLine(friendTableBuilder.ToString());

			OutputDisplayer.WriteLine(LogType.Info, $"Image Processing Time: {timeTaken.TotalMilliseconds} MS.");
			OutputDisplayer.WriteLine(LogType.Info, $"API Requests Time: {apiRequestTime.TotalSeconds} SEC.");
			Console.WriteLine(Divider);
		}
	}
}