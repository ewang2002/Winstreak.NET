using System;

namespace Winstreak.Core.Profile.Calculations
{
	public static class PlayerCalculator
	{
		/// <summary>
		/// Gets the "danger" score of the person.
		/// </summary>
		/// <param name="fkdrVal">The FKDR.</param>
		/// <param name="bedsBroken">The number of broken beds.</param>
		/// <returns>The "danger" score of this person.</returns>
		public static double GetScore((bool fdZero, double fkdr) fkdrVal, int bedsBroken)
		{
			var fkdr = fkdrVal.fdZero
				? 0
				: fkdrVal.fkdr;

			var fkdrScoreVal = 1 - 1 / (0.40 * Math.Pow(fkdr, 3) + 0.08333 * Math.Pow(fkdr, 2) + 1);
			var bedScoreVal = 1 - 1 / (1 / (double) 650 * bedsBroken + 1);
			return 75 * fkdrScoreVal + 25 * bedScoreVal;
		}

		/// <summary>
		/// Gets the "danger" score of this person. 
		/// </summary>
		/// <param name="fkdrVal">The FKDR.</param>
		/// <param name="finals">The number of finals.</param>
		/// <param name="bedsBroken">The number of broken beds.</param>
		/// <param name="level">The person's level.</param>
		/// <returns>The "danger" score of this person.</returns>
		public static double GetScore((bool fdZero, double fkdr) fkdrVal, int finals, double bedsBroken, int level)
		{
			var bedLevelVal = bedsBroken / (level + 1);
			var (fdZero, fkdr) = fkdrVal;
			var finalKillDeath = fdZero ? finals : fkdr;
			var finalVVal = Math.Pow(Math.Pow(finalKillDeath, 1.5) * (Math.Pow(finals, 0.5) / 2), 0.5)
				* (finalKillDeath / 2) + 1;

			return finalVVal * Math.Max(1, bedLevelVal); 
		}
	}
}