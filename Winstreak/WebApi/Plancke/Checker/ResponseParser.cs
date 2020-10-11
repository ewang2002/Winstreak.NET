using System.Collections.Generic;
using System.Linq;
using Winstreak.WebApi.Definition;

namespace Winstreak.WebApi.Plancke.Checker
{
	public class ResponseParser
	{
		public readonly IDictionary<string, string> Names;
		public int TotalBedsBroken { get; private set; }
		public int TotalFinalKills { get; private set; }
		public int TotalFinalDeaths { get; private set; }
		public int TotalWins { get; private set; }
		public int TotalLosses { get; private set; }
		public IList<string> ErroredPlayers { get; }

		/// <summary>
		/// Creates a new ResponseParser object, which should be used after going through the PlanckeApiRequester class.
		/// </summary>
		/// <param name="names">A dictionary containing names and their corresponding raw data.</param>
		public ResponseParser(IDictionary<string, string> names)
		{
			Names = names;
			ErroredPlayers = new List<string>();
		}

		/// <summary>
		/// Parses the raw HTML data for each name.
		/// </summary>
		/// <returns>Stats of each player in an easy-to-use format.</returns>
		public IList<BedwarsData> GetPlayerDataFromMap()
		{
			var namesToWorryAbout = new List<BedwarsData>();

			var totalBrokenBeds = 0;
			var totalFinalKills = 0;
			var totalFinalDeaths = 0;
			var totalWins = 0;
			var totalLosses = 0;

			foreach (var (key, value) in Names)
			{
				var data = new ResponseData(key, value)
					.Parse()
					.TotalDataInfo;

				if (data is { } nonNullData)
				{
					totalBrokenBeds += nonNullData.BrokenBeds;
					totalFinalKills += nonNullData.FinalKills;
					totalFinalDeaths += nonNullData.FinalDeaths;
					totalWins += nonNullData.Wins;
					totalLosses += nonNullData.Losses;

					namesToWorryAbout.Add(nonNullData);
				}
				else
					ErroredPlayers.Add(key);
			}

			TotalFinalKills = totalFinalKills;
			TotalBedsBroken = totalBrokenBeds;
			TotalFinalDeaths = totalFinalDeaths;
			TotalWins = totalWins;
			TotalLosses = totalLosses;

			return namesToWorryAbout;
		}
	}
}