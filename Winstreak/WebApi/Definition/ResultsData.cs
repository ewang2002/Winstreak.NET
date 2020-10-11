using System.Collections.Generic;
using System.Linq;
using Winstreak.Parsers.ImageParser;
using Winstreak.Utility.Calculations;

namespace Winstreak.WebApi.Definition
{
	public readonly struct TeamInfoResults
	{
		public readonly string Color;
		public readonly IList<BedwarsData> AvailablePlayers;
		public readonly IList<string> ErroredPlayers;
		public readonly double Score;

		public TeamInfoResults(
			TeamColor color,
			IList<BedwarsData> availablePlayers,
			IList<string> errored
		)
		{
			Color = color.ToString();
			AvailablePlayers = availablePlayers;
			ErroredPlayers = errored;
			Score = PlayerCalculator.CalculatePlayerThreatLevel(
				availablePlayers.Sum(x => x.Wins),
				availablePlayers.Sum(x => x.Losses),
				availablePlayers.Sum(x => x.FinalKills),
				availablePlayers.Sum(x => x.FinalDeaths),
				availablePlayers.Sum(x => x.BrokenBeds)
			);
		}
	}
}