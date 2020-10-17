using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Winstreak.DirectoryManager;
using Winstreak.Profile;
using static Winstreak.WebApi.ApiConstants;

namespace Winstreak.WebApi.Plancke
{
	public static class PlanckeApi
	{
		/// <summary>
		/// Gets profile data from Plancke. 
		/// </summary>
		/// <param name="name">The name to look up.</param>
		/// <returns>A tuple containing the name of the person and his or her profile (null if the player doesn't exist).</returns>
		public static async Task<(string name, PlayerProfile profile)> GetProfileFromPlancke(string name)
		{
			if (ApiClient.DefaultRequestHeaders.Contains("X-Forwarded-For"))
				ApiClient.DefaultRequestHeaders.Remove("X-Forwarded-For");

			ApiClient.DefaultRequestHeaders.Add("X-Forwarded-For", GenerateRandomIpAddress());
			// make request
			var response = await ApiClient.GetAsync($"https://plancke.io/hypixel/player/stats/{name}");

			if (response.StatusCode != HttpStatusCode.OK)
			{
				for (var attempts = 0; attempts < DirectoryWatcher.Config.RetryMax; ++attempts)
				{
					await Task.Delay(TimeSpan.FromMilliseconds(DirectoryWatcher.Config.RetryDelay));

					response = await ApiClient.GetAsync($"https://plancke.io/hypixel/player/stats/{name}");
					if (response.StatusCode == HttpStatusCode.OK)
						break;
				}
			}

			// parse time. 
			var strMessage = await response.Content.ReadAsStringAsync();

			// get player name
			var playerName = string.Empty;
			try
			{
				var nameOfWebsite = strMessage.Split("<title>")[1]
					.Split("</title>")[0]
					.Trim();

				// name exists
				if (nameOfWebsite.Contains('\''))
					playerName = nameOfWebsite.Split('\'')[0].Trim();
			}
			catch (Exception)
			{
				// an exception probably means the name doesn't exist. 
				return (name, null); 
			}

			// get player network level
			var networkLevel = -1.0;
			try
			{
				var tempLevel = strMessage.Split("<b>Level:</b>")[1]
					.Split("<br")[0]
					.Trim();

				if (double.TryParse(tempLevel, out var val))
					networkLevel = val;
			}
			catch (Exception)
			{
				// nothing to see here
			}

			var karma = -1;
			try
			{
				var tempKarma = strMessage.Split("Karma:</b>")[1]
					.Split("<br")[0]
					.Replace(",", "")
					.Trim();

				if (int.TryParse(tempKarma, out var val))
					karma = val;
			}
			catch (Exception)
			{
				// nothing to see here.
			}

			var firstLogin = DateTime.MinValue;
			try
			{
				var tempFirstLogin = strMessage.Split("Firstlogin: </b>")[1]
					.Split("<br")[0]
					.Trim()
					.Replace("EDT", "-4")
					.Replace("EST", "-5");

				firstLogin = DateTime.ParseExact(tempFirstLogin, "yyyy-MM-dd HH:mm z", CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				// nothing to see here. 
			}

			// parse stats, the hardest part 
			var bedwarsData = strMessage
				.Split(new[] { "Bed Wars </a>", "Bed Wars  </a>" }, StringSplitOptions.RemoveEmptyEntries)[1]
				.Split("Build Battle")[0];

			var bedwarsLevel = -1;
			try
			{
				var parseRes = int.TryParse(bedwarsData
					.Split("<ul class=\"list-unstyled\">")[1]
					.Split("<li><b>Level:</b>")[1]
					.Split("</li>")[0]
					.Trim(), out var lvl);

				if (parseRes)
					bedwarsLevel = lvl;
			}
			catch (Exception)
			{
				// ignored
			}

			var bedwarsWinstreak = -1;
			try
			{
				var parseResp = int.TryParse(bedwarsData
					.Split("<ul class=\"list-unstyled\">")[1]
					.Split("<li><b>Winstreak:</b>")[1]
					.Split("</li>")[0]
					.Trim(), out var ws);
				if (parseResp)
					bedwarsWinstreak = ws;
			}
			catch (Exception)
			{
				// ignored
			}

			// ====== GET BEDWARS STATS ======

			// clean up data
			bedwarsData = bedwarsData
				.Replace("<td style=\"border-right: 1px solid #f3f3f3\">", "")
				.Replace("<th scope=\"row\" style=\"border-right: 1px solid #f3f3f3\">", "");

			// get data for solos
			var soloData = bedwarsData
				.Split("Solo")[1]
				.Split("Doubles")[0]
				.Replace("</th><td>", "")
				.Replace("</td></tr><tr>", "");
			var soloDataArr = soloData
				.Replace(",", "")
				.Split(new[] { "</td>", "<td>" }, StringSplitOptions.RemoveEmptyEntries);
			soloDataArr = soloDataArr
				.Where(x => x != string.Empty)
				.ToArray();

			var kills = 0;
			var deaths = 0;
			var finalKills = 0;
			var finalDeaths = 0;
			var wins = 0;
			var losses = 0;
			var brokenBeds = 0;

			try
			{
				kills += int.Parse(soloDataArr[0]);
				deaths += int.Parse(soloDataArr[1]);
				finalKills += int.Parse(soloDataArr[3]);
				finalDeaths += int.Parse(soloDataArr[4]);
				wins += int.Parse(soloDataArr[6]);
				losses += int.Parse(soloDataArr[7]);
				brokenBeds += int.Parse(soloDataArr[9]);
			}
			catch (Exception)
			{
				// ignored 
			}

			// get data for doubles
			var doubleData = bedwarsData
				.Split("Doubles")[1]
				.Split("3v3v3v3")[0]
				.Replace("</th><td>", "")
				.Replace("</td></tr><tr>", "");
			var doubleDataArr = doubleData
				.Replace(",", "")
				.Split(new[] { "</td>", "<td>" }, StringSplitOptions.RemoveEmptyEntries);
			doubleDataArr = doubleDataArr
				.Where(x => x != string.Empty)
				.ToArray();

			try
			{
				kills += int.Parse(doubleDataArr[0]);
				deaths += int.Parse(doubleDataArr[1]);
				finalKills += int.Parse(doubleDataArr[3]);
				finalDeaths += int.Parse(doubleDataArr[4]);
				wins += int.Parse(doubleDataArr[6]);
				losses += int.Parse(doubleDataArr[7]);
				brokenBeds += int.Parse(doubleDataArr[9]);
			}
			catch (Exception)
			{
				// ignored
			}

			// get data for 3v3v3v3
			var threeData = bedwarsData
				.Split("3v3v3v3")[1]
				.Split("4v4v4v4")[0]
				.Replace("</th><td>", "")
				.Replace("</td></tr><tr>", "");
			var threeDataArr = threeData
				.Replace(",", "")
				.Split(new[] { "</td>", "<td>" }, StringSplitOptions.RemoveEmptyEntries);
			threeDataArr = threeDataArr
				.Where(x => x != string.Empty)
				.ToArray();

			try
			{
				kills += int.Parse(threeDataArr[0]);
				deaths += int.Parse(threeDataArr[1]);
				finalKills += int.Parse(threeDataArr[3]);
				finalDeaths += int.Parse(threeDataArr[4]);
				wins += int.Parse(threeDataArr[6]);
				losses += int.Parse(threeDataArr[7]);
				brokenBeds += int.Parse(threeDataArr[9]);
			}
			catch (Exception)
			{
				// ignored
			}

			// get data for 4v4v4v4
			var fourData = bedwarsData
				.Split("4v4v4v4")[1]
				.Split("4v4")[0]
				.Replace("</th><td>", "")
				.Replace("</td></tr><tr>", "");
			var fourDataArr = fourData
				.Replace(",", "")
				.Split(new[] { "</td>", "<td>" }, StringSplitOptions.RemoveEmptyEntries);
			fourDataArr = fourDataArr
				.Where(x => x != string.Empty)
				.ToArray();

			try
			{
				kills += int.Parse(fourDataArr[0]);
				deaths += int.Parse(fourDataArr[1]);
				finalKills += int.Parse(fourDataArr[3]);
				finalDeaths += int.Parse(fourDataArr[4]);
				wins += int.Parse(fourDataArr[6]);
				losses += int.Parse(fourDataArr[7]);
				brokenBeds += int.Parse(fourDataArr[9]);
			}
			catch (Exception)
			{
				// ignored
			}

			return (playerName,
				new PlayerProfile(playerName, networkLevel, karma, firstLogin,
					new BedwarsInformation(kills, deaths, finalKills, finalDeaths, wins, losses, brokenBeds,
						bedwarsWinstreak, bedwarsLevel)));
		}

		/// <summary>
		/// Requests data for more than 1 person.
		/// </summary>
		/// <param name="names">The names to look up.</param>
		/// <returns>A tuple containing all profiles and any names that resulted in an error.</returns>
		public static async Task<(IList<PlayerProfile> profiles, ISet<string> nicked)> GetMultipleProfilesFromPlancke(
			IList<string> names)
		{
			var profiles = new List<PlayerProfile>();
			var namesToCheck = new List<string>();

			foreach (var name in names)
			{
				if (CachedPlayerData.Contains(name))
				{
					profiles.Add(CachedPlayerData[name]);
					continue;
				}

				namesToCheck.Add(name);	
			}

			var requests = namesToCheck
				.Select(GetProfileFromPlancke)
				.ToArray();

			var profileData = await Task.WhenAll(requests);

			var nicked = new HashSet<string>();
			foreach (var (name, profile) in profileData)
			{
				if (profile == null)
				{
					nicked.Add(name);
					continue;
				}

				profiles.Add(profile);
			}

			return (profiles, nicked);
		}

		/// <summary>
		/// Generates a random IP address.
		/// </summary>
		/// <returns>A random IP address.</returns>
		private static string GenerateRandomIpAddress()
		{
			var r = new Random();
			return $"{r.Next(256)}.{r.Next(256)}.{r.Next(256)}.{r.Next(256)}";
		}
	}
}