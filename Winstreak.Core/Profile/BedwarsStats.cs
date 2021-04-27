namespace Winstreak.Core.Profile
{
	public readonly struct BedwarsStats
	{
		/// <summary>
		/// The number of kills this player has.
		/// </summary>
		public readonly int Kills;

		/// <summary>
		/// The number of deaths this player has.
		/// </summary>
		public readonly int Deaths;

		/// <summary>
		/// The number of final kills this player has.
		/// </summary>
		public readonly int FinalKills;

		/// <summary>
		/// The number of final deaths this player has.
		/// </summary>
		public readonly int FinalDeaths;

		/// <summary>
		/// The number of wins this player has.
		/// </summary>
		public readonly int Wins;

		/// <summary>
		/// The number of losses this player has.
		/// </summary>
		public readonly int Losses;

		/// <summary>
		/// The number of beds this player has broken.
		/// </summary>
		public readonly int BrokenBeds;

		/// <summary>
		/// A constructor that takes in stats for this player.
		/// </summary>
		/// <param name="kills">The number of kills.</param>
		/// <param name="deaths">The number of deaths.</param>
		/// <param name="finalKills">The number of final kills.</param>
		/// <param name="finalDeaths">The number of final deaths.</param>
		/// <param name="wins">The number of wins.</param>
		/// <param name="losses">The number of deaths.</param>
		/// <param name="beds">The number of beds broken.</param>
		public BedwarsStats(int kills, int deaths, int finalKills, int finalDeaths, int wins, int losses,
			int beds)
		{
			Kills = kills;
			Deaths = deaths;
			FinalDeaths = finalDeaths;
			FinalKills = finalKills;
			Wins = wins;
			Losses = losses;
			BrokenBeds = beds;
		}

		/// <summary>
		/// Gets the person's Win/Loss ratio. 
		/// </summary>
		/// <returns>A tuple containing two elements. If the first element is true, then the person's normal losses would be 0. If the first element is false, then there is a defined W/L ratio.</returns>
		public (bool lZero, double wlr) GetWinLossRatio() => Losses == 0
			// to avoid getting exception
			? (true, -1)
			: (false, Wins / (double) Losses);

		/// <summary>
		/// Gets the person's KDR. 
		/// </summary>
		/// <returns>A tuple containing two elements. If the first element is true, then the person's normal deaths would be 0. If the first element is false, then there is a defined KDR.</returns>
		public (bool dZero, double kdr) GetKdr() => Deaths == 0
			// to avoid getting exception
			? (true, -1)
			: (false, Kills / (double) Deaths);

		/// <summary>
		/// Gets the person's FKDR. 
		/// </summary>
		/// <returns>A tuple containing two elements. If the first element is true, then the person's final deaths would be 0. If the first element is false, then there is a defined FKDR.</returns>
		public (bool fdZero, double fkdr) GetFkdr() => FinalDeaths == 0
			// to avoid getting exception
			? (true, -1)
			: (false, FinalKills / (double) FinalDeaths);

		/// <summary>
		/// Adds two BedwarsInformation objects.
		/// </summary>
		/// <param name="b1">The first object.</param>
		/// <param name="b2">The second object.</param>
		/// <returns>The new object.</returns>
		public static BedwarsStats operator +(BedwarsStats b1, BedwarsStats b2)
			=> new(
				b1.Kills + b2.Kills,
				b1.Deaths + b2.Deaths,
				b1.FinalKills + b2.FinalKills,
				b1.FinalDeaths + b2.FinalDeaths,
				b1.Wins + b2.Wins,
				b1.Losses + b2.Losses,
				b1.BrokenBeds + b2.BrokenBeds
			);
	}
}