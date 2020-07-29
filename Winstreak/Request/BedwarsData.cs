namespace Winstreak.Request
{
	public struct BedwarsData
	{
		public readonly int Kills;
		public readonly int Deaths;
		public readonly int FinalKills;
		public readonly int FinalDeaths;
		public readonly int Wins;
		public readonly int Losses;
		public readonly int BrokenBeds;

		public BedwarsData(
			int kills,
			int deaths,
			int finalKills,
			int finalDeaths,
			int wins,
			int losses,
			int bedsBroken
		)
		{
			Kills = kills;
			Deaths = deaths;
			FinalKills = finalKills;
			FinalDeaths = finalDeaths;
			Wins = wins;
			Losses = losses;
			BrokenBeds = bedsBroken;
		}
	}
}