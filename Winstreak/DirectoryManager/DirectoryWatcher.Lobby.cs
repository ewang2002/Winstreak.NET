using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Winstreak.Utility.Calculations;
using Winstreak.Utility.ConsoleTable;
using Winstreak.WebApi.Definition;
using Winstreak.WebApi.Plancke;
using Winstreak.WebApi.Plancke.Checker;
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

			var nameResults = new List<BedwarsData>();

			// check hypixel api
			if (HypixelApi != null && ApiKeyValid)
			{
				var (responses, nicked, unableToSearch) = await HypixelApi
					.GetAllPlayersAsync(names.ToList());
				nickedPlayers = nicked.ToList();

				foreach (var resp in responses)
				{
					totalWins += resp.Wins;
					totalLosses += resp.Losses;
					totalBrokenBeds += resp.BrokenBeds;
					totalFinalKills += resp.FinalKills;
					totalFinalDeaths += resp.FinalDeaths;
					levels += resp.Level;

					CachedPlayerData.TryAdd(resp.Name, resp);
					nameResults.Add(resp);
				}

				// request leftover data from plancke
				var planckeApiRequester = new PlanckeApiRequester(unableToSearch);
				// parse data
				var nameData = await planckeApiRequester
					.SendRequestsAsync();
				var checker = new ResponseParser(nameData);

				foreach (var playerInfo in checker.GetPlayerDataFromMap())
				{
					totalFinalDeaths += playerInfo.FinalDeaths;
					totalFinalKills += playerInfo.FinalKills;
					totalBrokenBeds += playerInfo.BrokenBeds;
					totalWins += playerInfo.Wins;
					totalLosses += playerInfo.Losses;
					if (playerInfo.Level != -1)
						levels += playerInfo.Level;

					CachedPlayerData.TryAdd(playerInfo.Name, playerInfo);
					nameResults.Add(playerInfo);
				}

				nickedPlayers.AddRange(checker.ErroredPlayers);
			}
			else
			{
				// request data from plancke
				var planckeApiRequester = new PlanckeApiRequester(names.ToList());
				// parse data
				var nameData = await planckeApiRequester
					.SendRequestsAsync();
				var checker = new ResponseParser(nameData);

				foreach (var playerInfo in checker.GetPlayerDataFromMap())
				{
					totalFinalDeaths += playerInfo.FinalDeaths;
					totalFinalKills += playerInfo.FinalKills;
					totalBrokenBeds += playerInfo.BrokenBeds;
					totalWins += playerInfo.Wins;
					totalLosses += playerInfo.Losses;
					if (playerInfo.Level != -1)
						levels += playerInfo.Level;

					CachedPlayerData.TryAdd(playerInfo.Name, playerInfo);
					nameResults.Add(playerInfo);
				}

				nickedPlayers.AddRange(checker.ErroredPlayers.ToList());
			}

			nameResults = nameResults
				.OrderByDescending(SortBySpecifiedType())
				.ToList();

			var (friendGroups, nameFriendsUnable) = await GetGroups(nameResults);

			reqTime.Stop();
			var apiRequestTime = reqTime.Elapsed;

			// start parsing the data
			var tableBuilder = new Table(8)
				.AddRow("LVL", $"{names.Count} Players", "Finals", "Beds", "FKDR", "WS", "Score", "Assessment")
				.AddSeparator();
			foreach (var playerInfo in nameResults)
				tableBuilder.AddRow(
					playerInfo.Level == -1 ? "N/A" : playerInfo.Level.ToString(),
					Config.DangerousPlayers.Contains(playerInfo.Name.ToLower())
						? BackgroundBrightYellowAnsi + playerInfo.Name + ResetAnsi
						: playerInfo.Name,
					playerInfo.FinalKills,
					playerInfo.BrokenBeds,
					playerInfo.FinalDeaths == 0
						? "N/A"
						: Math.Round((double)playerInfo.FinalKills / playerInfo.FinalDeaths, 2)
							.ToString(CultureInfo.InvariantCulture),
					playerInfo.Winstreak == -1
						? "N/A"
						: playerInfo.Winstreak.ToString(),
					Math.Round(playerInfo.Score, 2),
					DetermineScoreMeaning(playerInfo.Score, true)
				);

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
			var ttlScore = PlayerCalculator.CalculatePlayerThreatLevel(totalWins, totalLosses,
				totalFinalKills, totalFinalDeaths, totalBrokenBeds);
			tableBuilder.AddRow(
				levels,
				"Total",
				totalFinalKills,
				totalBrokenBeds,
				totalLosses == 0
					? "N/A"
					: Math.Round((double)totalWins / totalLosses, 2)
						.ToString(CultureInfo.InvariantCulture),
				string.Empty,
				Math.Round(ttlScore, 2),
				DetermineScoreMeaning(ttlScore, false)
			);

			// group friends
			Table friendTableBuilder;
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
						friendTableBuilder.AddRow(
							member.Level,
							member.Name,
							member.FinalDeaths == 0
								? "N/A"
								: Math.Round((double)member.FinalKills / member.FinalDeaths, 2)
									.ToString(CultureInfo.InvariantCulture),
							Math.Round(member.Score, 2),
							DetermineScoreMeaning(member.Score, true)
						);
					}

					if (i + 1 != friendGroups.Count)
						friendTableBuilder.AddSeparator();
				}

				friendTableBuilder
					.AddSeparator()
					.AddRow(string.Empty, $"{nameFriendsUnable.Count} Names Not Checked", string.Empty, string.Empty,
						string.Empty);
			}
			else
			{
				if (HypixelApi != null && ApiKeyValid)
					friendTableBuilder = new Table(1)
						.AddRow(BackgroundBrightGreenAnsi + "No Friend Groups Detected!" + ResetAnsi)
						.AddSeparator()
						.AddRow($"{nameFriendsUnable.Count} Names Not Checked.");
				else
					friendTableBuilder = new Table(1)
						.AddRow("Hypixel API Not Used!");
			}

			Console.WriteLine(tableBuilder.ToString());
			Console.WriteLine(friendTableBuilder.ToString());
			Console.WriteLine($"[INFO] Image Processing Time: {timeTaken.TotalMilliseconds} Milliseconds.");
			Console.WriteLine($"[INFO] API Requests Time: {apiRequestTime.TotalSeconds} Sec.");
			Console.WriteLine(Divider);
		}
	}
}