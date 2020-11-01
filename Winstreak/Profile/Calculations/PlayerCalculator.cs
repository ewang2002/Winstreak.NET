using System;

namespace Winstreak.Profile.Calculations
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
	}
}