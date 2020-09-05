using Winstreak.Calculations;

namespace Winstreak.Request.Definition
{
	public readonly struct BedwarsData
	{
		public readonly string Name; 
		public readonly int Kills;
		public readonly int Deaths;
		public readonly int FinalKills;
		public readonly int FinalDeaths;
		public readonly int Wins;
		public readonly int Losses;
		public readonly int BrokenBeds;
		public readonly double Score; 

		public BedwarsData(
			string name,
			int kills,
			int deaths,
			int finalKills,
			int finalDeaths,
			int wins,
			int losses,
			int bedsBroken
		)
		{
			Name = name;
			Kills = kills;
			Deaths = deaths;
			FinalKills = finalKills;
			FinalDeaths = finalDeaths;
			Wins = wins;
			Losses = losses;
			BrokenBeds = bedsBroken;
			Score = PlayerCalculator.CalculatePlayerThreatLevel(wins, losses, finalKills, finalDeaths, bedsBroken);
		}
	}
}