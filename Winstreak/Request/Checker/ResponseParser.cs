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

		public ResponseParser(IDictionary<string, string> names) : this(names, 250, 750)
		{
		}

		public ResponseParser(IDictionary<string, string> names, int minBeds, int minFinals)
		{
			MinimumBrokenBeds = minBeds;
			MinimumFinalKills = minFinals;
			Names = names;
			ErroredPlayers = new List<string>();
		}

		public ResponseParser SetMinimumBrokenBedsNeeded(int minBeds)
		{
			MinimumBrokenBeds = minBeds;
			return this;
		}

		public ResponseParser SetMinimumFinalKillsNeeded(int minFin)
		{
			MinimumFinalKills = minFin;
			return this;
		}

		public IList<ResponseCheckerResults> GetNamesToWorryAbout()
		{
			IList<ResponseCheckerResults> namesToWorryAbout = new List<ResponseCheckerResults>();

			int totalBrokenBeds = 0;
			int totalFinalKills = 0;

			foreach (KeyValuePair<string, string> entry in Names)
			{
				BedwarsData? data = new ResponseData(entry.Key, entry.Value)
					.Parse()
					.TotalDataInfo;

				if (data is { } nonNullData)
				{
					totalBrokenBeds += nonNullData.BrokenBeds;
					totalFinalKills += nonNullData.FinalKills;

					if (nonNullData.BrokenBeds >= MinimumBrokenBeds || nonNullData.FinalKills >= MinimumFinalKills)
					{
						ResponseCheckerResults results = new ResponseCheckerResults(entry.Key, nonNullData.BrokenBeds, nonNullData.FinalKills);
						namesToWorryAbout.Add(results);
					}
				}
				else
				{
					ErroredPlayers.Add(entry.Key);
				}
			}

			this.TotalFinalKills = totalFinalKills;
			this.TotalBedsBroken = totalBrokenBeds;

			namesToWorryAbout = namesToWorryAbout
				.OrderByDescending(x => x.BedsBroken)
				.ToList();

			return namesToWorryAbout;
		}

		public IList<ResponseCheckerResults> GetPlayerDataFromMap()
		{
			IList<ResponseCheckerResults> namesToWorryAbout = new List<ResponseCheckerResults>();

			int totalBrokenBeds = 0;
			int totalFinalKills = 0;

			foreach (KeyValuePair<string, string> entry in Names)
			{
				BedwarsData? data = new ResponseData(entry.Key, entry.Value)
					.Parse()
					.TotalDataInfo;

				if (data is { } nonNullData)
				{
					TotalBedsBroken += nonNullData.BrokenBeds;
					TotalFinalKills += nonNullData.FinalKills;

					ResponseCheckerResults results = new ResponseCheckerResults(entry.Key, nonNullData.BrokenBeds, nonNullData.FinalKills);
					namesToWorryAbout.Add(results);
				}
				else
				{
					ErroredPlayers.Add(entry.Key);
				}
			}

			this.TotalFinalKills = totalFinalKills;
			this.TotalBedsBroken = totalBrokenBeds;

			namesToWorryAbout = namesToWorryAbout
				.OrderByDescending(x => x.BedsBroken)
				.ToList();

			return namesToWorryAbout;
		}
	}
}