using System;
using System.Collections.Generic;
using Winstreak.Profile.Calculations;
using Winstreak.WebApi.Hypixel.Definitions;

namespace Winstreak.Profile
{
	/// <summary>
	/// <para>This class contains information about a player. However, since this is designed primarily for Bedwars, there will also be Bedwars stats. Each <c>PlayerProfile</c> object should have associated Bedwars stats.</para>
	/// <para>Bear in mind that this class should be cached for later use. This is primarily because we don't want to hit the rate limit.</para>
	/// </summary>
	public sealed class PlayerProfile
	{
		/// <summary>
		/// The name of this person.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The Uuid of this person.
		/// </summary>
		public string Uuid { get; }

		/// <summary>
		/// When this person first joined Hypixel.
		/// </summary>
		public DateTime FirstJoined { get; }

		/// <summary>
		/// The amount of karma the person has. 
		/// </summary>
		public long Karma { get; }

		/// <summary>
		/// The person's network experience. 
		/// </summary>
		public double NetworkLevel { get; }

		/// <summary>
		/// This person's overall Bedwars statistics. 
		/// </summary>
		public BedwarsStats OverallBedwarsStats { get; }

		/// <summary>
		/// Bedwars stats for solos.
		/// </summary>
		public BedwarsStats EightOneBedwarsStats { get; }

		/// <summary>
		/// Bedwars stats for doubles.
		/// </summary>
		public BedwarsStats EightTwoBedwarsStats { get; }

		/// <summary>
		/// Bedwars stats for 3v3v3v3.
		/// </summary>
		public BedwarsStats FourThreeBedwarsStats { get; }

		/// <summary>
		/// Bedwars stats for 4v4v4v4.
		/// </summary>
		public BedwarsStats FourFourBedwarsStats { get; }

		/// <summary>
		/// The person's Bedwars level. 
		/// </summary>
		public int BedwarsLevel { get; }

		/// <summary>
		/// The person's current Bedwars winstreak.
		/// </summary>
		public int Winstreak { get; }

		/// <summary>
		/// A constructor that takes in the response object from Hypixel's API. 
		/// </summary>
		/// <param name="apiResponse">The object.</param>
		/// <exception cref="ArgumentException">Whether the <c>HypixelPlayerApiResponse</c> object indicated that the request wasn't successful.</exception>
		public PlayerProfile(HypixelPlayerApiResponse apiResponse)
		{
			if (!apiResponse.Success || apiResponse.Player == null)
				throw new ArgumentException("Request either wasn't successful or the player object was null.");

			Name = apiResponse.Player.DisplayName;
			Uuid = apiResponse.Player.Uuid;
			FirstJoined = new DateTime(1970, 1, 1)
				.AddMilliseconds(apiResponse.Player.FirstLogin);
			Karma = apiResponse.Player.Karma;
			NetworkLevel = Math.Sqrt(2 * apiResponse.Player.NetworkExp + 30625) / 50 - 2.5;

			if (apiResponse.Player.Stats?.Bedwars != null)
			{
				EightOneBedwarsStats = new BedwarsStats(
					(int) apiResponse.Player.Stats.Bedwars.SolosKills,
					(int) apiResponse.Player.Stats.Bedwars.SolosDeaths,
					(int) apiResponse.Player.Stats.Bedwars.SolosFinalKills,
					(int) apiResponse.Player.Stats.Bedwars.SolosFinalDeaths,
					(int) apiResponse.Player.Stats.Bedwars.SolosWins,
					(int) apiResponse.Player.Stats.Bedwars.SolosLosses,
					(int) apiResponse.Player.Stats.Bedwars.SolosBrokenBeds
				);

				EightTwoBedwarsStats = new BedwarsStats(
					(int) apiResponse.Player.Stats.Bedwars.DoublesKills,
					(int) apiResponse.Player.Stats.Bedwars.DoublesDeaths,
					(int) apiResponse.Player.Stats.Bedwars.DoublesFinalKills,
					(int) apiResponse.Player.Stats.Bedwars.DoublesFinalDeaths,
					(int) apiResponse.Player.Stats.Bedwars.DoublesWins,
					(int) apiResponse.Player.Stats.Bedwars.DoublesLosses,
					(int) apiResponse.Player.Stats.Bedwars.DoublesBrokenBeds
				);

				FourThreeBedwarsStats = new BedwarsStats(
					(int) apiResponse.Player.Stats.Bedwars.ThreesKills,
					(int) apiResponse.Player.Stats.Bedwars.ThreesDeaths,
					(int) apiResponse.Player.Stats.Bedwars.ThreesFinalKills,
					(int) apiResponse.Player.Stats.Bedwars.ThreesFinalDeaths,
					(int) apiResponse.Player.Stats.Bedwars.ThreesWins,
					(int) apiResponse.Player.Stats.Bedwars.ThreesLosses,
					(int) apiResponse.Player.Stats.Bedwars.ThreesBrokenBeds
				);

				FourFourBedwarsStats = new BedwarsStats(
					(int) apiResponse.Player.Stats.Bedwars.FoursKills,
					(int) apiResponse.Player.Stats.Bedwars.FoursDeaths,
					(int) apiResponse.Player.Stats.Bedwars.FoursFinalKills,
					(int) apiResponse.Player.Stats.Bedwars.FoursFinalDeaths,
					(int) apiResponse.Player.Stats.Bedwars.FoursWins,
					(int) apiResponse.Player.Stats.Bedwars.FoursLosses,
					(int) apiResponse.Player.Stats.Bedwars.FoursBrokenBeds
				);

				OverallBedwarsStats = EightOneBedwarsStats
				                      + EightTwoBedwarsStats
				                      + FourThreeBedwarsStats
				                      + FourFourBedwarsStats;

				BedwarsLevel = BedwarsExpLevel.GetLevelFromExp(apiResponse.Player.Stats.Bedwars.Experience);
				Winstreak = apiResponse.Player.Stats.Bedwars.Winstreak;
			}
			else
			{
				EightOneBedwarsStats = new BedwarsStats();
				EightTwoBedwarsStats = new BedwarsStats();
				FourThreeBedwarsStats = new BedwarsStats();
				FourFourBedwarsStats = new BedwarsStats();
				OverallBedwarsStats = EightOneBedwarsStats
				                      + EightTwoBedwarsStats
				                      + FourThreeBedwarsStats
				                      + FourFourBedwarsStats;

				BedwarsLevel = 0;
				Winstreak = 0;
			}
		}

		/// <summary>
		/// A constructor that takes in several items. This is normally called from Plancke's API.
		/// </summary>
		/// <param name="name">The name of the person.</param>
		/// <param name="level">The person's network level.</param>
		/// <param name="karma">The amount of karma this person has.</param>
		/// <param name="firstLogin">When the person first logged in.</param>
		/// <param name="stats">The person's stats. The first element is solos; the second element is doubles; the third element is 3v3v3v3s; the fourth element is 4v4v4v4s; the fifth element is overall.</param>
		/// <param name="bedwarsLevel">The person's bedwars level.</param>
		/// <param name="winstreak">The person's winstreak.</param>
		public PlayerProfile(string name,
			double level,
			long karma,
			DateTime firstLogin,
			IReadOnlyList<BedwarsStats> stats,
			int bedwarsLevel,
			int winstreak)
		{
			Name = name;
			NetworkLevel = level;
			FirstJoined = firstLogin;
			Karma = karma;

			EightOneBedwarsStats = stats[0];
			EightTwoBedwarsStats = stats[1];
			FourThreeBedwarsStats = stats[2];
			FourFourBedwarsStats = stats[3];
			OverallBedwarsStats = stats[4];

			BedwarsLevel = bedwarsLevel;
			Winstreak = winstreak;
		}

		/// <summary>
		/// Returns the amount of network experience this person has.
		/// </summary>
		/// <returns>The network experience.</returns>
		public double GetNetworkExp() => (Math.Pow(50 * (NetworkLevel + 2.5), 2) - 30625) / 2;
	}
}