using System;
using Winstreak.Calculations;
using static Winstreak.Calculations.BedwarsExpLevel;
using Winstreak.WebApi.Hypixel.Definitions;

namespace Winstreak.WebApi.Plancke.Definition
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
		public readonly int Level; 

		public BedwarsData(
			string name,
			int kills,
			int deaths,
			int finalKills,
			int finalDeaths,
			int wins,
			int losses,
			int bedsBroken,
			int level
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
			Level = level;
		}

		public BedwarsData(HypixelPlayerApiResponse resp)
		{
			if (resp.Player == null)
				throw new ArgumentNullException(nameof(resp));
			Name = resp.Player.DisplayName;

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
			Score = PlayerCalculator.CalculatePlayerThreatLevel(Wins, Losses, FinalKills, FinalDeaths, BrokenBeds);
			Level = GetLevelFromExp(resp.Player.Stats.Bedwars.Experience);
		}
	}
}