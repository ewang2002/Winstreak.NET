using System;

namespace Winstreak.Calculations
{
	public static class PlayerCalculator
	{
		/// <summary>
		/// Calculates the player's threat level based on stats. The formulas used can be found <see href="https://www.desmos.com/calculator/lv1ym6kpm3">here</see>. I designed the formulas to give Final KDR, Broken Beds, and Win/Losses a certain "weight" so one characteristic of someone's stats doesn't determine whether that someone is a tryhard or not. 
		/// </summary>
		/// <param name="wins">The number of wins.</param>
		/// <param name="losses">The number of losses.</param>
		/// <param name="finalKills">The total final kills.</param>
		/// <param name="finalDeaths">The total final losses.</param>
		/// <param name="brokenBeds">The total broken beds.</param>
		/// <returns>The threat level, from 0 to 100.</returns>
		public static double CalculatePlayerThreatLevel(int wins, int losses, int finalKills, int finalDeaths,
			int brokenBeds)
			=> 10 * GetWinLossLevel(wins, losses)
			   + 45 * GetFinalKillDeathLevel(finalKills, finalDeaths)
			   + 45 * GetBrokenBedLevel(brokenBeds);

		/// <summary>
		/// Determines the threat level, from 0 to 1, based on win/loss ratio. 
		/// </summary>
		/// <param name="wins">The total wins.</param>
		/// <param name="losses">The total losses.</param>
		/// <returns>The win/loss threat level, from a scale of 0 to 1.</returns>
		public static double GetWinLossLevel(int wins, int losses)
		{
			var winLossRatio = (double) wins / losses;
			return 1 - 1 / ((double) 3 / 4 * Math.Pow(winLossRatio, 5) + 1);
		}

		/// <summary>
		/// Determines the threat level, from 0 to 1, based on final kill/death ratio.
		/// </summary>
		/// <param name="fk">The total final kills.</param>
		/// <param name="fd">The total final deaths.</param>
		/// <returns>The final kill/death threat level, from a scale of 0 to 1.</returns>
		public static double GetFinalKillDeathLevel(int fk, int fd)
		{
			var fkdr = (double) fk / fd;
			return 1 - 1 / (2 * Math.Pow(fkdr, 2) + 1);
		}

		/// <summary>
		/// Determines the threat level, from 0 to 1, based on broken beds.
		/// </summary>
		/// <param name="brokenBed">The number of broken beds.</param>
		/// <returns>The broken beds threat level, from a scale of 0 to 1.</returns>
		public static double GetBrokenBedLevel(int brokenBed)
			=> 1 - 1 / ((double) 1 / 650 * brokenBed + 1);
	}
}