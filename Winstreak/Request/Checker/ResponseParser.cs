using System.Collections.Generic;
using System.Linq;

namespace Winstreak.Request.Checker
{
	public class ResponseParser
	{
		public readonly IDictionary<string, string> Names;
		public int MinimumBrokenBeds { get; private set; }
		public int MinimumFinalKills { get; private set; }
		public int TotalBedsBroken { get; private set; }
		public int TotalFinalKills { get; private set; }
		public IList<string> ErroredPlayers { get; private set;  }

		/// <summary>
		/// Creates a new ResponseParser object, which should be used after going through the PlanckeApiRequester class.
		/// </summary>
		/// <param name="names">A dictionary containing names and their corresponding raw data.</param>
		public ResponseParser(IDictionary<string, string> names) : this(names, 250, 750)
		{
		}

		/// <summary>
		/// Creates a new ResponseParser object, which should be used after going through the PlanckeApiRequester class.
		/// </summary>
		/// <param name="names">A dictionary containing names and their corresponding raw data.</param>
		/// <param name="minBeds">The minimum number of beds a person must have to be a tryhard.</param>
		/// <param name="minFinals">The minimum number of finals a person must have to be a tryhard.</param>
		public ResponseParser(IDictionary<string, string> names, int minBeds, int minFinals)
		{
			MinimumBrokenBeds = minBeds;
			MinimumFinalKills = minFinals;
			Names = names;
			ErroredPlayers = new List<string>();
		}

		/// <summary>
		/// Sets the minimum number of broken beds needed for someone to be a tryhard.
		/// </summary>
		/// <param name="minBeds">The minimum number of beds a person must have to be a tryhard.</param>
		/// <returns>This object.</returns>
		public ResponseParser SetMinimumBrokenBedsNeeded(int minBeds)
		{
			MinimumBrokenBeds = minBeds;
			return this;
		}

		/// <summary>
		/// Sets the minimum number of finals a person must have to be a tryhard.
		/// </summary>
		/// <param name="minFin">The minimum number of finals a person must have to be a tryhard.</param>
		/// <returns>This object.</returns>
		public ResponseParser SetMinimumFinalKillsNeeded(int minFin)
		{
			MinimumFinalKills = minFin;
			return this;
		}

		/// <summary>
		/// Parses the raw HTML data for each name. This will only return a list of people to worry about.
		/// </summary>
		/// <returns>Stats of tryhard players in an easy-to-use format.</returns>
		public IList<ResponseCheckerResults> GetNamesToWorryAbout()
		{
			var namesToWorryAbout = new List<ResponseCheckerResults>();

			var totalBrokenBeds = 0;
			var totalFinalKills = 0;

			foreach (var (key, value) in Names)
			{
				var data = new ResponseData(key, value)
					.Parse()
					.TotalDataInfo;

				if (data is { } nonNullData)
				{
					totalBrokenBeds += nonNullData.BrokenBeds;
					totalFinalKills += nonNullData.FinalKills;

					if (nonNullData.BrokenBeds < MinimumBrokenBeds &&
					    nonNullData.FinalKills < MinimumFinalKills) 
						continue;
					var results = new ResponseCheckerResults(key, nonNullData.BrokenBeds, nonNullData.FinalKills);
					namesToWorryAbout.Add(results);
				}
				else
				{
					ErroredPlayers.Add(key);
				}
			}

			TotalFinalKills = totalFinalKills;
			TotalBedsBroken = totalBrokenBeds;

			namesToWorryAbout = namesToWorryAbout
				.OrderByDescending(x => x.BedsBroken)
				.ToList();

			return namesToWorryAbout;
		}

		/// <summary>
		/// Parses the raw HTML data for each name.
		/// </summary>
		/// <returns>Stats of each player in an easy-to-use format.</returns>
		public IList<ResponseCheckerResults> GetPlayerDataFromMap()
		{
			var namesToWorryAbout = new List<ResponseCheckerResults>();

			var totalBrokenBeds = 0;
			var totalFinalKills = 0;

			foreach (var (key, value) in Names)
			{
				var data = new ResponseData(key, value)
					.Parse()
					.TotalDataInfo;

				if (data is { } nonNullData)
				{
					totalBrokenBeds += nonNullData.BrokenBeds;
					totalFinalKills += nonNullData.FinalKills;

					var results = new ResponseCheckerResults(key, nonNullData.BrokenBeds, nonNullData.FinalKills);
					namesToWorryAbout.Add(results);
				}
				else
					ErroredPlayers.Add(key);
			}

			TotalFinalKills = totalFinalKills;
			TotalBedsBroken = totalBrokenBeds;

			namesToWorryAbout = namesToWorryAbout
				.OrderByDescending(x => x.BedsBroken)
				.ToList();

			return namesToWorryAbout;
		}
	}
}