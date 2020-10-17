﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Winstreak.Profile;
using Winstreak.Profile.Calculations;
using Winstreak.Utility.ConsoleTable;
using Winstreak.WebApi.Plancke;
using static Winstreak.WebApi.ApiConstants;
using static Winstreak.Utility.ConsoleTable.AnsiConstants;

namespace Winstreak.DirectoryManager
{
	public static partial class DirectoryWatcher
	{
		/// <summary>
		/// Checks to see if a lobby is suitable enough for an "easy" game.
		/// </summary>
		/// <param name="names">The names to check.</param>
		/// <param name="timeTaken">The time taken for the screenshot to actually be processed.</param>
		/// <returns>Nothing.</returns>
		private static async Task ProcessLobbyScreenshotAsync(IList<string> names, TimeSpan timeTaken)
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

			var nameResults = new List<PlayerProfile>();

			// check hypixel api
			if (HypixelApi != null && ApiKeyValid)
			{
				var (responses, nicked, unableToSearch) = await HypixelApi
					.GetAllPlayersAsync(names.ToList());
				nickedPlayers = nicked.ToList();

				foreach (var resp in responses)
				{
					totalWins += resp.BedwarsStats.Wins;
					totalLosses += resp.BedwarsStats.Losses;
					totalBrokenBeds += resp.BedwarsStats.BrokenBeds;
					totalFinalKills += resp.BedwarsStats.FinalKills;
					totalFinalDeaths += resp.BedwarsStats.FinalDeaths;
					levels += resp.BedwarsStats.BedwarsLevel;

					CachedPlayerData.TryAdd(resp.Name, resp);
					nameResults.Add(resp);
				}

				// request leftover data from plancke
				var (profilesPlancke, nickedPlancke) = await PlanckeApi
					.GetMultipleProfilesFromPlancke(unableToSearch);

				foreach (var playerInfo in profilesPlancke)
				{
					totalFinalDeaths += playerInfo.BedwarsStats.FinalDeaths;
					totalFinalKills += playerInfo.BedwarsStats.FinalKills;
					totalBrokenBeds += playerInfo.BedwarsStats.BrokenBeds;
					totalWins += playerInfo.BedwarsStats.Wins;
					totalLosses += playerInfo.BedwarsStats.Losses;
					if (playerInfo.BedwarsStats.BedwarsLevel != -1)
						levels += playerInfo.BedwarsStats.BedwarsLevel;

					CachedPlayerData.TryAdd(playerInfo.Name, playerInfo);
					nameResults.Add(playerInfo);
				}

				nickedPlayers.AddRange(nickedPlancke);
			}
			else
			{
				// request data from plancke
				var (profilesPlancke, nickedPlancke) = await PlanckeApi
					.GetMultipleProfilesFromPlancke(names.ToList());

				foreach (var playerInfo in profilesPlancke)
				{
					totalFinalDeaths += playerInfo.BedwarsStats.FinalDeaths;
					totalFinalKills += playerInfo.BedwarsStats.FinalKills;
					totalBrokenBeds += playerInfo.BedwarsStats.BrokenBeds;
					totalWins += playerInfo.BedwarsStats.Wins;
					totalLosses += playerInfo.BedwarsStats.Losses;
					if (playerInfo.BedwarsStats.BedwarsLevel != -1)
						levels += playerInfo.BedwarsStats.BedwarsLevel;

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
				var fkdr = playerInfo.BedwarsStats.GetFkdr();
				var score = playerInfo.BedwarsStats.GetScore();
				tableBuilder.AddRow(
					playerInfo.BedwarsStats.BedwarsLevel == -1
						? "N/A"
						: playerInfo.BedwarsStats.BedwarsLevel.ToString(),
					Config.DangerousPlayers.Contains(playerInfo.Name.ToLower())
						? BackgroundBrightYellowAnsi + playerInfo.Name + ResetAnsi
						: playerInfo.Name,
					playerInfo.BedwarsStats.FinalKills,
					playerInfo.BedwarsStats.BrokenBeds,
					fkdr.fdZero ? "N/A" : Math.Round(fkdr.fkdr, 2).ToString(CultureInfo.InvariantCulture),
					playerInfo.BedwarsStats.Winstreak == -1
						? "N/A"
						: playerInfo.BedwarsStats.Winstreak.ToString(),
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
				totalFinalDeaths == 0
					? (true, -1.0)
					: (false, totalFinalKills / (double) totalFinalDeaths),
				totalBrokenBeds
			);

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
							var fkdr = member.BedwarsStats.GetFkdr();
							friendTableBuilder.AddRow(
								member.BedwarsStats.BedwarsLevel,
								member.Name,
								fkdr.fdZero ? "N/A" : Math.Round(fkdr.fkdr, 2).ToString(CultureInfo.InvariantCulture),
								Math.Round(member.BedwarsStats.GetScore(), 2),
								DetermineScoreMeaning(member.BedwarsStats.GetScore(), true)
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

			// check for potential "interesting" and/or suspicious accounts
			var susAccountNotes = new Dictionary<string, string>();
			foreach (var playerProfile in nameResults)
			{
				// check time
				var notes = new HashSet<string>();
				var joinedHypixel = DateTime.Now - playerProfile.FirstJoined;

				// 30 days
				if (joinedHypixel.TotalMilliseconds < 2.628e+9)
					notes.Add($"First Login: {joinedHypixel.Days} Days Ago.");

				if (playerProfile.Karma <= 150)
					notes.Add($"Karma Amount: {playerProfile.Karma}");

				if (playerProfile.NetworkLevel < 10)
					notes.Add($"Network Level: {Math.Round(playerProfile.NetworkLevel, 1)}");

				if (notes.Count == 0)
					continue;

				susAccountNotes.Add(playerProfile.Name, string.Join("\n", notes));
			}

			if (susAccountNotes.Count != 0)
			{
				var susTable = new Table(2)
					.AddRow("Name", "Reason")
					.AddSeparator();
				foreach (var (name, reason) in susAccountNotes)
					susTable.AddRowContainingNewLine(name, $"{reason}\n");

				Console.WriteLine(susTable.ToString());
			}

			Console.WriteLine($"[INFO] Image Processing Time: {timeTaken.TotalMilliseconds} Milliseconds.");
			Console.WriteLine($"[INFO] API Requests Time: {apiRequestTime.TotalSeconds} Sec.");
			Console.WriteLine(Divider);
		}
	}
}