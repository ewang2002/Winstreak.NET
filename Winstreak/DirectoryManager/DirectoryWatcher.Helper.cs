using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Winstreak.Utility;
using Winstreak.WebApi.Definition;
using Winstreak.WebApi.Hypixel.Definitions;
using Winstreak.WebApi.Mojang;
using static Winstreak.Utility.ConsoleTable.AnsiConstants;
using static Winstreak.WebApi.ApiConstants;

namespace Winstreak.DirectoryManager
{
	public static partial class DirectoryWatcher
	{
		#region Minor Stuff

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

		private static string GamemodeIntToStr()
			=> Mode switch
			{
				12 => "Solos/Doubles",
				34 => "3v3v3v3s/4v4v4v4s/4v4s",
				_ => throw new ArgumentOutOfRangeException(nameof(Mode), "Gamemode must either be 34 or 12.")
			};

		private static Func<BedwarsData, double> SortBySpecifiedType()
			=> SortingType switch
			{
				SortType.Beds => data => data.BrokenBeds,
				SortType.Finals => data => data.FinalKills,
				SortType.Fkdr => data =>
					data.FinalDeaths == 0 ? data.FinalKills : data.FinalKills / (double) data.FinalDeaths,
				SortType.Score => data => data.Score,
				SortType.Winstreak => data => data.Winstreak,
				SortType.Level => data => data.Level,
				_ => throw new ArgumentOutOfRangeException()
			};

		private static Func<TeamInfoResults, double> TeamSortBySpecifiedType()
			=> SortingType switch
			{
				SortType.Beds => data => data.AvailablePlayers.Sum(x => x.BrokenBeds),
				SortType.Finals => data => data.AvailablePlayers.Sum(x => x.FinalKills),
				SortType.Fkdr => data =>
				{
					var fd = data.AvailablePlayers.Sum(x => x.FinalDeaths);
					var fk = data.AvailablePlayers.Sum(x => x.FinalKills);
					return fd == 0 ? fk : fk / (double) fd;
				},
				SortType.Score => data => data.Score,
				SortType.Winstreak => data => data.AvailablePlayers.Sum(x => x.Winstreak),
				SortType.Level => data => data.AvailablePlayers.Sum(x => x.Level),
				_ => throw new ArgumentOutOfRangeException()
			};

		#endregion

		/// <summary>
		/// Gets all groups based on friends.
		/// </summary>
		/// <param name="nameResults">The names to check.</param>
		/// <returns>A tuple containing two elements -- one element with all groups and the other element with names that couldn't be checked.</returns>
		public static async Task<(IList<IList<BedwarsData>> friendGroups, HashSet<string> nameFriendsUnable)>
			GetGroups(IList<BedwarsData> nameResults)
		{
			var nameFriendsUnable = new HashSet<string>();
			// groups of friends
			var friendGroups = new List<IList<BedwarsData>>();

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
		private static int GetGroupIndex(IList<IList<BedwarsData>> friendGroups, IList<string> friendErrored,
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