using System;
using Winstreak.WebApi.Definition;
using Winstreak.WebApi.Hypixel.Definitions;

namespace Winstreak.WebApi
{
	public readonly struct BedwarsInformation
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
		/// This player's Bedwars level.
		/// </summary>
		public readonly int BedwarsLevel;

		/// <summary>
		/// The number of consecutive wins this player has.
		/// </summary>
		public readonly int Winstreak;

		/// <summary>
		/// A constructor that takes in all stats for this player.
		/// </summary>
		/// <param name="kills">The number of kills.</param>
		/// <param name="deaths">The number of deaths.</param>
		/// <param name="finalKills">The number of final kills.</param>
		/// <param name="finalDeaths">The number of final deaths.</param>
		/// <param name="wins">The number of wins.</param>
		/// <param name="losses">The number of deaths.</param>
		/// <param name="beds">The number of beds broken.</param>
		/// <param name="winstreak">Current winstreak across all games.</param>
		/// <param name="level">Current bedwars level.</param>
		public BedwarsInformation(int kills, int deaths, int finalKills, int finalDeaths, int wins, int losses, int beds, int winstreak, double level)
		{
			Kills = kills;
			Deaths = deaths;
			FinalDeaths = finalDeaths;
			FinalKills = finalKills;
			Wins = wins;
			Losses = losses;
			BrokenBeds = beds;
			Winstreak = winstreak;
			BedwarsLevel = (int) level;
		}

		/// <summary>
		/// A constructor that takes in a <c>HypixelPlayerApiResponse</c> object.
		/// </summary>
		/// <param name="resp">The response from Hypixel's API.</param>
		public BedwarsInformation(HypixelPlayerApiResponse resp)
		{
			if (resp.Player == null)
				throw new ArgumentNullException(nameof(resp));

			if (resp.Player.Stats?.Bedwars == null)
			{
				Kills = 0;
				Deaths = 0;
				FinalDeaths = 0;
				FinalKills = 0;
				Wins = 0;
				Losses = 0;
				BrokenBeds = 0;
				BedwarsLevel = 0;
				Winstreak = 0;
				return;
			}

			Kills = (int) (resp.Player.Stats.Bedwars.SolosKills
			               + resp.Player.Stats.Bedwars.DoublesKills
			               + resp.Player.Stats.Bedwars.ThreesKills
			               + resp.Player.Stats.Bedwars.FoursKills);
			Deaths = (int) (resp.Player.Stats.Bedwars.SolosDeaths
			                + resp.Player.Stats.Bedwars.DoublesDeaths
			                + resp.Player.Stats.Bedwars.ThreesDeaths
			                + resp.Player.Stats.Bedwars.FoursDeaths);
			FinalKills = (int) (resp.Player.Stats.Bedwars.SolosFinalKills
			                    + resp.Player.Stats.Bedwars.DoublesFinalKills
			                    + resp.Player.Stats.Bedwars.ThreesFinalKills
			                    + resp.Player.Stats.Bedwars.FoursFinalKills);
			FinalDeaths = (int) (resp.Player.Stats.Bedwars.SolosFinalDeaths
			                     + resp.Player.Stats.Bedwars.DoublesFinalDeaths
			                     + resp.Player.Stats.Bedwars.ThreesFinalDeaths
			                     + resp.Player.Stats.Bedwars.FoursFinalDeaths);
			Wins = (int) (resp.Player.Stats.Bedwars.SolosWins
			              + resp.Player.Stats.Bedwars.DoublesWins
			              + resp.Player.Stats.Bedwars.ThreesWins
			              + resp.Player.Stats.Bedwars.FoursWins);
			Losses = (int) (resp.Player.Stats.Bedwars.SolosLosses
			                + resp.Player.Stats.Bedwars.DoublesLosses
			                + resp.Player.Stats.Bedwars.ThreesLosses
			                + resp.Player.Stats.Bedwars.FoursLosses);
			BrokenBeds = (int) (resp.Player.Stats.Bedwars.SolosBrokenBeds
			                    + resp.Player.Stats.Bedwars.DoublesBrokenBeds
			                    + resp.Player.Stats.Bedwars.ThreesBrokenBeds
			                    + resp.Player.Stats.Bedwars.FoursBrokenBeds);
			Winstreak = resp.Player.Stats.Bedwars.Winstreak;
			BedwarsLevel = GetLevelFromExp(resp.Player.Stats.Bedwars.Experience);
		}

		/// <summary>
		/// Gets the "danger" score of the person.
		/// </summary>
		/// <returns>The "danger" score of this person.</returns>
		public double GetScore()
		{
			var fkdr = GetFkdr();
			var fkdrScoreVal = FinalDeaths == 0
				? 0
				: 1 - 1 / (2 * Math.Pow(fkdr, 2) + 1);

			var bedScoreVal = 1 - 1 / (1 / (double) 650 * BrokenBeds + 1);

			return 58 * fkdrScoreVal + 42 * bedScoreVal;
		}

		/// <summary>
		/// Gets the person's FKDR. 
		/// </summary>
		/// <returns>If no final deaths, -1; otherwise, the FKDR.</returns>
		public double GetFkdr() => FinalDeaths == 0
			// to avoid getting exception
			? -1
			: FinalKills / (double)FinalDeaths;

		#region const, static methods and variables

		// private constant fields, used for calculations
		private const int EasyLevels = 4;
		private const int EasyLevelsXp = 7000;
		private const long XpPerPrestige = (long) 96 * 5000 * EasyLevelsXp;
		private const int LevelsPerPrestige = 100;
		private const int HighestPrestige = 10;

		// static methods

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
		private static int GetLevelRespectingPrestige(int level)
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

		#endregion
	}
}