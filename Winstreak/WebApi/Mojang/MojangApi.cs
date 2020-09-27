using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static Winstreak.WebApi.ApiConstants;

namespace Winstreak.WebApi.Mojang
{
	public static class MojangApi
	{
		/// <summary>
		/// The maximum number of requests that can be made in "HypixelRateLimit"
		/// </summary>
		public static int MaximumRequestsInRateLimit = 600;

		/// <summary>
		/// The amount of time before the rate limit is reset. 
		/// </summary>
		public static TimeSpan HypixelRateLimit = TimeSpan.FromMinutes(10);

		/// <summary>
		/// Caching Names & UUID.
		/// </summary>
		public static ConcurrentDictionary<string, string> NameUuid = new ConcurrentDictionary<string, string>();

		/// <summary>
		/// Gets the Uuid from the player's name.
		/// </summary>
		/// <param name="name">The name of the player.</param>
		/// <returns>The Uuid, if the name exists. Otherwise, an empty string.</returns>
		public static async Task<string> GetUuidFromPlayerName(string name)
		{
			if (NameUuid.ContainsKey(name.ToLower()))
				return NameUuid[name];

			using var reqMsgInfo = new HttpRequestMessage
			{
				RequestUri = new Uri($"https://api.mojang.com/users/profiles/minecraft/{name}"),
				Method = HttpMethod.Get
			};

			using var response = await ApiClient.SendAsync(reqMsgInfo);
			// we can get no content as a possibility 
			if (response.StatusCode != HttpStatusCode.OK)
				return string.Empty;

			var content = await response.Content.ReadAsStringAsync();
			var parsedResp = JsonConvert.DeserializeObject<MojangApiResponse>(content);
			NameUuid.TryAdd(name.ToLower(), parsedResp.Id);

			return parsedResp.Id;
		}
	}
}