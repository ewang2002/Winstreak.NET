using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Winstreak.Cli.Utility;
using Winstreak.Core.Profile;
using static Winstreak.Core.WebApi.CachedData;
using Winstreak.Core.WebApi.Hypixel.Definitions;
using Winstreak.Core.WebApi.Mojang;
using static Winstreak.Cli.Utility.ConsoleTable.AnsiConstants;

namespace Winstreak.Cli.DirectoryManager
{
	public static partial class DirectoryWatcher
	{
		#region Minor Stuff

		/// <summary>
		/// Determines what the score means in the context of the situation.
		/// </summary>
		/// <param name="score">The score.</param>
		/// <param name="isPlayer">Whether the score is referring to a player or the lobby.</param>
		/// <returns>What the score means in the situation.</returns>
		private static string DetermineScoreMeaning(double score, bool isPlayer)
		{
			if (score <= 20)
				return TextGreenAnsi + (isPlayer ? "Bad" : "Safe") + ResetAnsi;
			if (score > 20 && score <= 40)
				return TextBrightGreenAnsi + (isPlayer ? "Decent" : "Pretty Safe") + ResetAnsi;
			if (score > 40 && score <= 60)
				return TextBrightYellowAnsi + (isPlayer ? "Good" : "Somewhat Safe") + ResetAnsi;
			if (score > 60 && score <= 80)
				return TextYellowAnsi + (isPlayer ? "Professional" : "Not Safe") + ResetAnsi;
			return TextRedAnsi + (isPlayer ? "Tryhard" : "Leave Now") + ResetAnsi;
		}

		/// <summary>
		/// Returns a function that can be used to sort Bedwars stats.
		/// </summary>
		/// <returns>The function.</returns>
		private static Func<PlayerProfile, double> SortBySpecifiedType()
			=> SortingType switch
			{
				SortType.Beds => data => data.OverallBedwarsStats.BrokenBeds,
				SortType.Finals => data => data.OverallBedwarsStats.FinalKills,
				SortType.Fkdr => data =>
					data.OverallBedwarsStats.FinalDeaths == 0
						? data.OverallBedwarsStats.FinalKills
						: data.OverallBedwarsStats.FinalKills / (double) data.OverallBedwarsStats.FinalDeaths,
				SortType.Score => data => data.OverallBedwarsStats.GetScore(),
				SortType.Winstreak => data => data.Winstreak,
				SortType.Level => data => data.BedwarsLevel,
				_ => throw new ArgumentOutOfRangeException()
			};

		/// <summary>
		/// Returns a function that can be used to sort team stats.
		/// </summary>
		/// <returns>The function.</returns>
		private static Func<TeamProfile, double> TeamSortBySpecifiedType()
			=> SortingType switch
			{
				SortType.Beds => data => data.PlayersInTeam.Sum(x => x.OverallBedwarsStats.BrokenBeds),
				SortType.Finals => data => data.PlayersInTeam.Sum(x => x.OverallBedwarsStats.FinalKills),
				SortType.Fkdr => data =>
				{
					var fd = data.PlayersInTeam.Sum(x => x.OverallBedwarsStats.FinalDeaths);
					var fk = data.PlayersInTeam.Sum(x => x.OverallBedwarsStats.FinalKills);
					return fd == 0 ? fk : fk / (double) fd;
				},
				SortType.Score => data => data.CalculateScore(),
				SortType.Winstreak => data => data.PlayersInTeam.Sum(x => x.Winstreak),
				SortType.Level => data => data.PlayersInTeam.Sum(x => x.BedwarsLevel),
				_ => throw new ArgumentOutOfRangeException()
			};

		#endregion

		/// <summary>
		/// Gets all groups based on friends.
		/// </summary>
		/// <param name="nameResults">The names to check.</param>
		/// <returns>A tuple containing two elements -- one element with all groups and the other element with names that couldn't be checked.</returns>
		private static async Task<(IList<IList<PlayerProfile>> friendGroups, HashSet<string> nameFriendsUnable)>
			GetGroups(IList<PlayerProfile> nameResults)
		{
			var nameFriendsUnable = new HashSet<string>();
			// groups of friends
			var friendGroups = new List<IList<PlayerProfile>>();

			if (HypixelApi is null || !ApiKeyValid)
				return (friendGroups, nameFriendsUnable);

			var friendsData = new List<(string uuid, FriendsApiResponse friends)>();
			var namesNeededForFriends = new HashSet<(string name, string uuid)>();
			foreach (var playerData in nameResults)
			{
				// this will only be empty if
				// requested through plancke, which is a
				// possibility considering rate limit. 
				if (NameUuid.ContainsKey(playerData.Name))
				{
					namesNeededForFriends.Add((playerData.Name, NameUuid[playerData.Name]));
					continue;
				}

				if (playerData.Uuid == string.Empty)
				{
					var mojangResp = await MojangApi.GetUuidFromPlayerNameAsync(playerData.Name);
					if (mojangResp == string.Empty)
					{
						nameFriendsUnable.Add(playerData.Name);
						continue;
					}

					namesNeededForFriends.Add((playerData.Name, mojangResp));
					continue;
				}

				if (CachedFriendsData.Contains(playerData.Uuid))
				{
					friendsData.Add((playerData.Uuid, CachedFriendsData[playerData.Uuid]));
					continue;
				}

				namesNeededForFriends.Add((playerData.Name, playerData.Uuid));
			}

			var (responses, unableToSearch) = await HypixelApi
				.GetAllFriendsAsync(namesNeededForFriends.Select(x => x.uuid)
					.ToList());

			foreach (var invalidUuid in unableToSearch)
			{
				var name = namesNeededForFriends
					.Where(x => x.uuid == invalidUuid)
					.ToArray();
				if (!name.Any())
					continue;

				nameFriendsUnable.Add(name.First().name);
			}

			friendsData.AddRange(responses);

			// sort each name into friend groups
			// friendsData should only contain names from the particular lobby
			var friendsName = new List<IList<string>>();
			foreach (var (uuid, recordFriends) in responses)
			{
				var nameUuid = namesNeededForFriends
					.Where(x => x.uuid == uuid)
					.ToArray();
				if (nameUuid.Length == 0)
					continue;

				var group = new HashSet<string>
				{
					nameUuid[0].name
				};

				foreach (var record in recordFriends.Records)
				{
					var uuidToFocusOn = record.UuidSender == uuid
						? record.UuidReceiver
						: record.UuidSender;

					var valInLobby = namesNeededForFriends
						.Where(x => x.uuid == uuidToFocusOn)
						.ToArray();

					// not in this lobby
					if (valInLobby.Length == 0)
						continue;

					group.Add(valInLobby[0].name);
				}

				friendsName.Add(group.ToList());
			}

			var groups = ListUtil.GetGroups(ListUtil.GetGroups(friendsName))
				.Where(x => x.Count > 1)
				.ToList();

			friendGroups.AddRange(groups.Select(@group => (from member in @group
				select nameResults.Where(x => x.Name == member)
					.ToArray()
				into q
				where q.Length != 0
				select q[0]).ToList()));

			return (friendGroups, nameFriendsUnable);
		}

		/// <summary>
		/// Gets the index + 1 at which the name exists. 
		/// </summary>
		/// <param name="friendGroups">The friend groups.</param>
		/// <param name="friendErrored">The people that couldn't be searched due to rate limiting issues.</param>
		/// <param name="name">The name to look for.</param>
		/// <returns>-2 if the name was errored. -1 if the name doesn't belong to any group. index + 1 if the name is found in a group.</returns>
		private static int GetGroupIndex(IList<IList<PlayerProfile>> friendGroups, IList<string> friendErrored,
			string name)
		{
			if (friendErrored.Contains(name))
				return -2;

			for (var i = 0; i < friendGroups.Count; ++i)
			{
				var poss = friendGroups[i].Where(x => x.Name == name).ToArray();
				if (poss.Length == 1)
					return i + 1;
			}

			return -1;
		}

		/// <summary>
		/// Checks if a Minecraft log message is valid.
		/// </summary>
		/// <param name="text">The unparsed text.</param>
		/// <param name="res">The parsed text.</param>
		/// <returns>Whether the log message is valid.</returns>
		private static bool IsValidLogMessage(string text, out string res)
		{
			res = null;
			if (!text.Contains("[CHAT]")) return false;
#if DEBUG && PRINT
			Console.WriteLine(text);
			Console.WriteLine("========");
#endif

			var ranges = new[]
			{
				// [Client thread/INFO]:
				(11, 11 + 21),
				// [main/INFO]:
				(11, 11 + 12)
			};

			(int min, int max) minMax = (-1, -1);
			var targetStr = string.Empty;

			foreach (var (min, max) in ranges)
			{
				if (text.Length < max || !text[min..max].EndsWith("/INFO]:"))
					continue;
				minMax = (min, max);
				targetStr = text[min..max];
				break;
			}

			if (minMax.max == -1 && minMax.min == -1)
				return false;

			var fullyParsedStr = new StringBuilder();
			var isValid = false;
			foreach (var line in text.Split(Environment.NewLine))
			{
				var parsedLine = line;
				if (parsedLine[minMax.min..minMax.max] == targetStr)
					parsedLine = parsedLine[minMax.max..].Trim();
				if (parsedLine.StartsWith("[CHAT]"))
				{
					fullyParsedStr.Append(parsedLine[6..].Trim()).AppendLine();
					isValid = true;
					continue;
				}

				fullyParsedStr.Append(parsedLine).AppendLine();
			}

			if (!isValid)
				return false;

			res = fullyParsedStr.ToString().Trim();
			return true;
		}
	}
}