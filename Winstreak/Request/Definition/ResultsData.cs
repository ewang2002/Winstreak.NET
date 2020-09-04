using System.Collections.Generic;
using Winstreak.Parser;

namespace Winstreak.Request.Definition
{
	public readonly struct ResponseCheckerResult
	{
		public readonly string Name;
		public readonly int BedsBroken;
		public readonly int FinalKills;
		public readonly double Score; 

		public ResponseCheckerResult(string name, int bedsDestroyed, int finalKills, double score)
		{
			Name = name;
			BedsBroken = bedsDestroyed;
			FinalKills = finalKills;
			Score = score;
		}
    }

	public readonly struct TeamInfoResults
	{
		public readonly string Color;
		public readonly IList<ResponseCheckerResult> AvailablePlayers;
		public readonly IList<string> ErroredPlayers;
		public readonly int TotalFinalKills;
		public readonly int TotalBrokenBeds;

		public TeamInfoResults(
			TeamColors color,
			IList<ResponseCheckerResult> availablePlayers,
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