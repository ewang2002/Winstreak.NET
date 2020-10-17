﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Winstreak.Profile;
using Winstreak.Utility;
using Winstreak.WebApi;
using Winstreak.WebApi.Hypixel.Definitions;
using Winstreak.WebApi.Mojang;
using static Winstreak.Utility.ConsoleTable.AnsiConstants;
using static Winstreak.WebApi.ApiConstants;

namespace Winstreak.DirectoryManager
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
		/// Gets the current gamemode as a string format that can be displayed.
		/// </summary>
		/// <returns>The current, written-out gamemode.</returns>
		private static string GamemodeIntToStr()
			=> Mode switch
			{
				12 => "Solos/Doubles",
				34 => "3v3v3v3s/4v4v4v4s/4v4s",
				_ => throw new ArgumentOutOfRangeException(nameof(Mode), "Gamemode must either be 34 or 12.")
			};

		/// <summary>
		/// Returns a function that can be used to sort Bedwars stats.
		/// </summary>
		/// <returns>The function.</returns>
		private static Func<PlayerProfile, double> SortBySpecifiedType()
			=> SortingType switch
			{
				SortType.Beds => data => data.BedwarsStats.BrokenBeds,
				SortType.Finals => data => data.BedwarsStats.FinalKills,
				SortType.Fkdr => data =>
					data.BedwarsStats.FinalDeaths == 0 
						? data.BedwarsStats.FinalKills 
						: data.BedwarsStats.FinalKills / (double) data.BedwarsStats.FinalDeaths,
				SortType.Score => data => data.BedwarsStats.GetScore(),
				SortType.Winstreak => data => data.BedwarsStats.Winstreak,
				SortType.Level => data => data.BedwarsStats.BedwarsLevel,
				_ => throw new ArgumentOutOfRangeException()
			};

		/// <summary>
		/// Returns a function that can be used to sort team stats.
		/// </summary>
		/// <returns>The function.</returns>
		private static Func<TeamProfile, double> TeamSortBySpecifiedType()
			=> SortingType switch
			{
				SortType.Beds => data => data.PlayersInTeam.Sum(x => x.BedwarsStats.BrokenBeds),
				SortType.Finals => data => data.PlayersInTeam.Sum(x => x.BedwarsStats.FinalKills),
				SortType.Fkdr => data =>
				{
					var fd = data.PlayersInTeam.Sum(x => x.BedwarsStats.FinalDeaths);
					var fk = data.PlayersInTeam.Sum(x => x.BedwarsStats.FinalKills);
					return fd == 0 ? fk : fk / (double) fd;
				},
				SortType.Score => data => data.CalculateScore(),
				SortType.Winstreak => data => data.PlayersInTeam.Sum(x => x.BedwarsStats.Winstreak),
				SortType.Level => data => data.PlayersInTeam.Sum(x => x.BedwarsStats.BedwarsLevel),
				_ => throw new ArgumentOutOfRangeException()
			};

		#endregion

		/// <summary>
		/// Gets all groups based on friends.
		/// </summary>
		/// <param name="nameResults">The names to check.</param>
		/// <returns>A tuple containing two elements -- one element with all groups and the other element with names that couldn't be checked.</returns>
		public static async Task<(IList<IList<PlayerProfile>> friendGroups, HashSet<string> nameFriendsUnable)>
			GetGroups(IList<PlayerProfile> nameResults)
		{
			var nameFriendsUnable = new HashSet<string>();
			// groups of friends
			var friendGroups = new List<IList<PlayerProfile>>();

			if (HypixelApi == null || !ApiKeyValid)
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
	}
}