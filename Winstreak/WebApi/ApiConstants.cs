using System.Collections.Concurrent;
using System.Net.Http;
using Winstreak.Profile;
using Winstreak.WebApi.Hypixel.Definitions;

namespace Winstreak.WebApi
{
	public static class ApiConstants
	{
		/// <summary>
		/// Caching Names & UUID.
		/// </summary>
		public static readonly ConcurrentDictionary<string, string> NameUuid;

		/// <summary>
		/// The cached data.
		/// </summary>
		public static readonly CacheDictionary<string, PlayerProfile> CachedPlayerData;

		/// <summary>
		/// The cached friends data. K = Uuid, V = FriendsApiResponse
		/// </summary>
		public static readonly CacheDictionary<string, FriendsApiResponse> CachedFriendsData;

		/// <summary>
		/// The cached guild data. K = Uuid, V = FriendsApiResponse
		/// </summary>
		public static readonly CacheDictionary<string, GuildsApiResponse> CachedGuildData; 

		/// <summary>
		/// The API Client.
		/// </summary>
		public static readonly HttpClient ApiClient;

		/// <summary>
		/// The constructor for this method.
		/// </summary>
		static ApiConstants()
		{
			ApiClient = new HttpClient();
			ApiClient.DefaultRequestHeaders.Add("User-Agent",
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36");

			CachedPlayerData = new CacheDictionary<string, PlayerProfile>();
			CachedFriendsData = new CacheDictionary<string, FriendsApiResponse>();
			CachedGuildData = new CacheDictionary<string, GuildsApiResponse>();
			NameUuid = new ConcurrentDictionary<string, string>();
		}
	}
}