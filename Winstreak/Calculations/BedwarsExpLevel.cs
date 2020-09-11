using System;

namespace Winstreak.Calculations
{
	public static class BedwarsExpLevel
	{
		private const int EasyLevels = 4;
		private const int EasyLevelsXp = 7000;
		private const long XpPerPrestige = (long) 96 * 5000 * EasyLevelsXp;
		private const int LevelsPerPrestige = 100;
		private const int HighestPrestige = 10;

		/// <summary>
		/// Gets the Bedwars EXP from level.
		/// </summary>
		/// <param name="level">The level.</param>
		/// <returns>The corresponding Bedwars EXP.</returns>
		public static double GetExpFromLevel(int level)
		{
			if (level == 0)
				return 0;

			var respectedLevel = GetLevelRespectingPrestige(level);
			if (respectedLevel > EasyLevels)
				return 5000;

			return respectedLevel switch
			{
				1 => 500,
				2 => 1000,
				3 => 2000,
				4 => 3500,
				_ => 5000
			};
		}

		/// <summary>
		/// Gets the level corresponding to prestige level.
		/// </summary>
		/// <param name="level">The level.</param>
		/// <returns>The prestige level.</returns>
		public static int GetLevelRespectingPrestige(int level)
		{
			return level > HighestPrestige * LevelsPerPrestige
				? level - HighestPrestige * LevelsPerPrestige
				: level % LevelsPerPrestige;
		}

		/// <summary>
		/// Gets the level from Bedwars experience.
		/// </summary>
		/// <param name="exp">The experience.</param>
		/// <returns>The corresponding Bedwars level.</returns>
		public static int GetLevelFromExp(long exp)
		{
			var prestige = Math.Floor(exp / (double) XpPerPrestige);
			var level = prestige * LevelsPerPrestige;
			var expWithoutPrestige = exp - prestige * XpPerPrestige;

			for (var i = 1; i <= EasyLevels; ++i)
			{
				var expForEasyLevel = GetExpFromLevel(i);
				if (expWithoutPrestige < expForEasyLevel)
					break;

				level++;
				expWithoutPrestige -= expForEasyLevel;
			}

			return (int) (level + Math.Floor(expWithoutPrestige / 5000));
		}
	}
}