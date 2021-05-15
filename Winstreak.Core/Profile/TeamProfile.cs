using System.Collections.Generic;
using System.Linq;
using Winstreak.Core.Profile.Calculations;

namespace Winstreak.Core.Profile
{
	public sealed class TeamProfile
	{
		/// <summary>
		/// All the VALID players in the team.
		/// </summary>
		public IList<PlayerProfile> PlayersInTeam { get; }

		/// <summary>
		/// All the NICKED (or errored) players in the team.
		/// </summary>
		public IList<string> NickedPlayers { get; }

		/// <summary>
		/// The team's color.
		/// </summary>
		public string TeamColor { get; }

		/// <summary>
		/// A constructor for the TeamProfile class.
		/// </summary>
		/// <param name="color">The team color.</param>
		/// <param name="players">The valid players.</param>
		/// <param name="nicked">The nicked players.</param>
		public TeamProfile(string color, IList<PlayerProfile> players,
			IList<string> nicked)
		{
			PlayersInTeam = players;
			TeamColor = color;
			NickedPlayers = nicked;
		}

		/// <summary>
		/// Calculates the team's "threat" score. 
		/// </summary>
		/// <returns>The team's "threat" score.</returns>
		public double CalculateScore()
		{
			var sumFd = PlayersInTeam.Sum(x => x.OverallBedwarsStats.FinalDeaths);
			var sumFk = PlayersInTeam.Sum(x => x.OverallBedwarsStats.FinalKills);
			var sumBeds = PlayersInTeam.Sum(x => x.OverallBedwarsStats.BrokenBeds);

			return PlayerCalculator.GetScore(
				sumFd == 0
					? (true, -1.0)
					: (false, sumFk / (double)sumFd),
				sumBeds
			);
		}
	}
}