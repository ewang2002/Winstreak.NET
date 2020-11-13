using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Winstreak.Profile;
using Winstreak.WebApi.Hypixel.Definitions;
using static Winstreak.WebApi.ApiConstants;

namespace Winstreak.WebApi.Hypixel
{
	public class HypixelApi
	{
		/// <summary>
		/// The maximum number of requests that can be made in "HypixelRateLimit"
		/// </summary>
		public static readonly int MaximumRequestsInRateLimit = 120;

		/// <summary>
		/// The amount of time before the rate limit is reset. 
		/// </summary>
		public static readonly TimeSpan HypixelRateLimit = TimeSpan.FromMinutes(1);

		/// <summary>
		/// The timer.
		/// </summary>
		public Timer RateLimitTimer { get; set; }

		/// <summary>
		/// The number of requests made within the time period.
		/// </summary>
		public int RequestsMade { get; set; }

		private readonly string _apiKey;

		/// <summary>
		/// Hypixel API constructor.
		/// </summary>
		/// <param name="apiKey">The API key.</param>
		public HypixelApi(string apiKey)
		{
			RequestsMade = 0;
			_apiKey = apiKey;
			RateLimitTimer = new Timer
			{
				Enabled = false,
				AutoReset = false,
				Interval = HypixelRateLimit.TotalMilliseconds
			};
		}

		/// <summary>
		/// Checks and makes sure the API key is valid.
		/// </summary>
		/// <returns>Returns an object containing information about whether the API key is valid or not.</returns>
		public async Task<ValidateApiResponse> ValidateApiKeyAsync()
			=> await SendRequestAsync<ValidateApiResponse>("key?");

		/// <summary>
		/// Sends a request to Hypixel.
		/// </summary>
		/// <typeparam name="T">The class to deserialize the JSON to.</typeparam>
		/// <param name="urlInfo">Any arguments; for example, "player?name=name</param>
		/// <returns>The .NET object corresponding to type "T".</returns>
		public async Task<T> SendRequestAsync<T>(string urlInfo)
		{
			if (RequestsMade + 1 > MaximumRequestsInRateLimit)
				throw new Exception("You have hit the rate limit.");

			using var reqMsgInfo = new HttpRequestMessage
			{
				RequestUri = new Uri($"https://api.hypixel.net/{urlInfo}&key={_apiKey}"),
				Method = HttpMethod.Get
			};

			using var resp = await ApiClient.SendAsync(reqMsgInfo);

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

			var str = await resp.Content.ReadAsStringAsync();
			if (str == string.Empty)
				throw new Exception("No response data to parse.");

			return JsonConvert.DeserializeObject<T>(str);
		}

		/// <summary>
		/// Gets player data from Hypixel's API. 
		/// </summary>
		/// <param name="name">The name to look up.</param>
		/// <returns>The results.</returns>
		public async Task<HypixelPlayerApiResponse> GetPlayerInfoAsync(string name) 
			=> await SendRequestAsync<HypixelPlayerApiResponse>($"player?name={name}&");

		/// <summary>
		/// Gets friend data from Hypixel's API.
		/// </summary>
		/// <param name="uuid">The Uuid to look up.</param>
		/// <returns>The result.</returns>
		public async Task<FriendsApiResponse> GetFriendsInfoAsync(string uuid)
			=> await SendRequestAsync<FriendsApiResponse>($"friends?uuid={uuid}&");

		/// <summary>
		/// Gets guild data from Hypixel's API.
		/// </summary>
		/// <param name="uuid">The Uuid to look up.</param>
		/// <returns>The result.</returns>
		public async Task<GuildsApiResponse> GetGuildInfoAsync(string uuid)
			=> await SendRequestAsync<GuildsApiResponse>($"guild?player={uuid}&");

		/// <summary>
		/// Processes a list of Uuids for friends data.
		/// </summary>
		/// <param name="uuids">The Uuids to look up.</param>
		/// <returns>A tuple containing all responses and list of uuids that couldn't be processed due to rate limit issues.</returns>
		public async Task<(IList<(string uuid, FriendsApiResponse friends)> responses, 
				IList<string> unableToSearch)>
			GetAllFriendsAsync(IList<string> uuids)
		{
			var responses = new List<(string, FriendsApiResponse)>();
			var unableToSearch = new List<string>();
			var actualUuidToLookUp = new List<string>();

			var tempReqMade = RequestsMade;
			foreach (var uuid in uuids)
			{
				if (CachedFriendsData.Contains(uuid))
				{
					responses.Add((uuid, CachedFriendsData[uuid]));
					continue;
				}

				if (tempReqMade + 1 > MaximumRequestsInRateLimit)
				{
					unableToSearch.Add(uuid);
					continue;
				}

				tempReqMade++;
				actualUuidToLookUp.Add(uuid);
			}

			if (actualUuidToLookUp.Count == 0)
				return (responses, unableToSearch);

			var requests = actualUuidToLookUp
				.Select(GetFriendsInfoAsync)
				.ToArray();

			var completedRequests = await Task.WhenAll(requests);
			for (var i = 0; i < completedRequests.Length; i++)
			{
				var finishedReq = completedRequests[i];
				if (!finishedReq.Success || finishedReq.Records == null)
					continue;

				responses.Add((actualUuidToLookUp[i], finishedReq));
				// TODO is this the correct way to do it? 
				CachedFriendsData.TryAdd(actualUuidToLookUp[i], finishedReq, TimeSpan.FromMinutes(45));
			}
			return (responses, unableToSearch);
		}

		/// <summary>
		/// Processes a list of Uuids for guild data.
		/// </summary>
		/// <param name="uuids">The Uuids to look up.</param>
		/// <returns>A tuple containing all responses and list of names that couldn't be processed due to rate limit issues.</returns>
		public async Task<(IList<GuildsApiResponse> responses,
				IList<string> unableToSearch)>
			GetAllGuildsAsync(IList<string> uuids)
		{
			var responses = new List<GuildsApiResponse>();
			var unableToSearch = new List<string>();
			var actualUuidToLookUp = new List<string>();

			var tempReqMade = RequestsMade;
			foreach (var uuid in uuids)
			{
				if (CachedGuildData.Contains(uuid))
				{
					responses.Add(CachedGuildData[uuid]);
					continue;
				}

				if (tempReqMade + 1 > MaximumRequestsInRateLimit)
				{
					unableToSearch.Add(uuid);
					continue;
				}

				tempReqMade++;
				actualUuidToLookUp.Add(uuid);
			}

			if (actualUuidToLookUp.Count == 0)
				return (responses, unableToSearch);

			var requests = actualUuidToLookUp
				.Select(GetGuildInfoAsync)
				.ToArray();

			var completedRequests = await Task.WhenAll(requests);
			for (var i = 0; i < completedRequests.Length; i++)
			{
				var finishedReq = completedRequests[i];
				if (!finishedReq.Success || finishedReq.Guild == null) 
					continue;

				responses.Add(finishedReq);
				CachedGuildData.TryAdd(actualUuidToLookUp[i], finishedReq, TimeSpan.FromHours(1));
			}
			return (responses, unableToSearch);
		}

		/// <summary>
		/// Processes a list of names. 
		/// </summary>
		/// <param name="names">The names to look up.</param>
		/// <returns>A tuple containing three elements: one element consisting of all valid responses; another element consisting of all nicked players; a third element consisting of the names that couldn't be searched due to rate limit issues.</returns>
		public async Task<(IList<PlayerProfile> responses,
				IList<string> nicked,
				IList<string> unableToSearch)>
			GetAllPlayersAsync(IList<string> names)
		{
			var nicked = new List<string>();
			var unableToSearch = new List<string>();
			var responses = new List<PlayerProfile>();

			// names that wont error due to rate limit
			var actualNamesToLookUp = new List<string>();
			var tempReqMade = RequestsMade;
			foreach (var name in names)
			{
				if (CachedPlayerData.Contains(name))
				{
					responses.Add(CachedPlayerData[name]);
					continue;
				}

				if (tempReqMade + 1 > MaximumRequestsInRateLimit)
				{
					unableToSearch.Add(name);
					continue;
				}

				tempReqMade++;
				actualNamesToLookUp.Add(name);
			}

			if (actualNamesToLookUp.Count == 0) 
				return (responses, nicked, unableToSearch);

			var requests = actualNamesToLookUp
				.Select(GetPlayerInfoAsync)
				.ToArray();

			var completedRequests = await Task.WhenAll(requests);
			for (var i = 0; i < completedRequests.Length; i++)
			{
				var finishedReq = completedRequests[i];
				if (finishedReq.Success && finishedReq.Player != null)
				{
					responses.Add(new PlayerProfile(finishedReq));
					CachedPlayerData.TryAdd(finishedReq.Player.DisplayName, new PlayerProfile(finishedReq));
					NameUuid.TryAdd(finishedReq.Player.DisplayName, finishedReq.Player.Uuid);
				}
				else
					nicked.Add(actualNamesToLookUp[i]);
			}

			return (responses, nicked, unableToSearch);
		}
	}
}