using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Winstreak.WebApi.Mojang.Definitions;
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
		public static TimeSpan MojangRateLimit = TimeSpan.FromMinutes(10);

		/// <summary>
		/// The rate limit timer. 
		/// </summary>
		public static Timer RateLimitTimer;

		/// <summary>
		/// The number of requests made. 
		/// </summary>
		public static int RequestsMade = 0;

		/// <summary>
		/// The static constructor for this static class. 
		/// </summary>
		static MojangApi()
		{
			RateLimitTimer = new Timer
			{
				Enabled = false,
				AutoReset = false,
				Interval = MojangRateLimit.TotalMilliseconds
			};
		}

		/// <summary>
		/// Returns the player name from the specified Uuid. 
		/// </summary>
		/// <param name="uuid">The Uuid.</param>
		/// <returns>The player's name, if the Uuid is valid. Otherwise, an empty string.</returns>
		public static async Task<string> GetPlayerNameFromUuidAsync(string uuid)
		{
			if (RequestsMade + 1 > MaximumRequestsInRateLimit)
				throw new Exception("You have hit the rate limit.");

			foreach (var (key, value) in NameUuid)
			{
				if (value == uuid)
					return key;
			}

			using var reqMsgInfo = new HttpRequestMessage
			{
				RequestUri = new Uri($"https://api.mojang.com/user/profiles/{uuid}/names"),
				Method = HttpMethod.Get
			};

			using var response = await ApiClient.SendAsync(reqMsgInfo);

			if (!RateLimitTimer.Enabled)
			{
				RateLimitTimer.Start();
				RateLimitTimer.Elapsed += (sender, args) =>
				{
					RateLimitTimer.Stop();
					RequestsMade = 0;
				};
			}
			RequestsMade++;

			if (response.StatusCode == HttpStatusCode.BadRequest)
				return string.Empty;

			var content = await response.Content.ReadAsStringAsync();
			var parsedResp = JsonConvert.DeserializeObject<UuidToNameResponse[]>(content);
			var actualName = parsedResp[^1];
			NameUuid.TryAdd(actualName.Name, uuid);

			return actualName.Name;
		}

		/// <summary>
		/// Gets the Uuid from the player's name.
		/// </summary>
		/// <param name="name">The name of the player.</param>
		/// <returns>The Uuid, if the name exists. Otherwise, an empty string.</returns>
		public static async Task<string> GetUuidFromPlayerNameAsync(string name)
		{
			if (RequestsMade + 1 > MaximumRequestsInRateLimit)
				throw new Exception("You have hit the rate limit.");

			if (NameUuid.ContainsKey(name.ToLower()))
				return NameUuid[name];

			using var reqMsgInfo = new HttpRequestMessage
			{
				RequestUri = new Uri($"https://api.mojang.com/users/profiles/minecraft/{name}"),
				Method = HttpMethod.Get
			};

			using var response = await ApiClient.SendAsync(reqMsgInfo);

			// start timer
			if (!RateLimitTimer.Enabled)
			{
				RateLimitTimer.Start();
				RateLimitTimer.Elapsed += (sender, args) =>
				{
					RateLimitTimer.Stop();
					RequestsMade = 0;
				};
			}
			RequestsMade++;

			// we can get no content as a possibility 
			if (response.StatusCode != HttpStatusCode.OK)
				return string.Empty;

			var content = await response.Content.ReadAsStringAsync();
			var parsedResp = JsonConvert.DeserializeObject<NameToUuidResponse>(content);
			NameUuid.TryAdd(name.ToLower(), parsedResp.Id);

			return parsedResp.Id;
		}
	}
}