using System;
using Winstreak.WebApi.Hypixel.Definitions;

namespace Winstreak.WebApi
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
		public string Name { get; private set; }

		/// <summary>
		/// The Uuid of this person.
		/// </summary>
		public string Uuid { get; private set; }

		/// <summary>
		/// When this person first joined Hypixel.
		/// </summary>
		public DateTime FirstJoined { get; private set; }

		/// <summary>
		/// The amount of karma the person has. 
		/// </summary>
		public long Karma { get; private set; }

		/// <summary>
		/// The person's network experience. 
		/// </summary>
		public double NetworkLevel { get; private set; }

		/// <summary>
		/// This person's Bedwars statistics. 
		/// </summary>
		public BedwarsInformation BedwarsStats { get; private set; }

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
			BedwarsStats = new BedwarsInformation(apiResponse);
		}

		/// <summary>
		/// A constructor that takes in several items. This is normally called from Plancke's API.
		/// </summary>
		/// <param name="name">The name of the person.</param>
		/// <param name="level">The person's network level.</param>
		/// <param name="karma">The amount of karma this person has.</param>
		/// <param name="firstLogin">When the person first logged in.</param>
		/// <param name="stats">The person's stats.</param>
		public PlayerProfile(string name, 
			double level, 
			long karma, 
			DateTime firstLogin,
			BedwarsInformation stats)
		{
			Name = name;
			NetworkLevel = level;
			FirstJoined = firstLogin;
			Karma = karma;
			BedwarsStats = stats;
		}

		/// <summary>
		/// Returns the amount of network experience this person has.
		/// </summary>
		/// <returns>The network experience.</returns>
		public double GetNetworkExp() => (Math.Pow(50 * (NetworkLevel + 2.5), 2) - 30625) / 2;
	}
}