using System;
using System.Collections.Generic;
using Winstreak.Parser;

namespace Winstreak.Request.Checker
{
	public struct ResponseCheckerResults
	{
		public readonly string Name;
		public readonly int BedsBroken;
		public readonly int FinalKills;

		public ResponseCheckerResults(string name, int bedsDestroyed, int finalKills)
		{
			Name = name;
			BedsBroken = bedsDestroyed;
			FinalKills = finalKills;
		}
    }

	public struct TeamInfoResults
	{
		public readonly string Color;
		public readonly IList<ResponseCheckerResults> AvailablePlayers;
		public readonly IList<String> ErroredPlayers;
		public readonly int TotalFinalKills;
		public readonly int TotalBrokenBeds;

		public TeamInfoResults(
			TeamColors color,
			List<ResponseCheckerResults> availablePlayers,
			IList<string> errored,
			int totalFinals,
			int totalBroken
		)
		{
			Color = color.ToString();
			AvailablePlayers = availablePlayers;
			ErroredPlayers = errored;
			TotalBrokenBeds = totalBroken;
			TotalFinalKills = totalFinals;
		}
    }
}