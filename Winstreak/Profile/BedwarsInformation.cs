using System;
using Winstreak.Profile.Calculations;
using Winstreak.WebApi.Hypixel.Definitions;

namespace Winstreak.Profile
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
			BedwarsLevel = BedwarsExpLevel.GetLevelFromExp(resp.Player.Stats.Bedwars.Experience);
		}

		/// <summary>
		/// Gets the person's FKDR. 
		/// </summary>
		/// <returns>A tuple containing two elements. If the first element is true, then the person's final deaths would be 0. If the first element is false, then there is a defined FKDR.</returns>
		public (bool fdZero, double fkdr) GetFkdr() => FinalDeaths == 0
			// to avoid getting exception
			? (true, -1)
			: (false, FinalKills / (double)FinalDeaths);

		/// <summary>
		/// Gets the person's "perceived" danger score. 
		/// </summary>
		/// <returns>The person's "perceived" danger score.</returns>
		public double GetScore()
			=> PlayerCalculator.GetScore(GetFkdr(), BrokenBeds);
	}
}