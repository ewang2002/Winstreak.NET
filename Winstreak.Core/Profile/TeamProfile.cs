using System.Collections.Generic;

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
	}
}