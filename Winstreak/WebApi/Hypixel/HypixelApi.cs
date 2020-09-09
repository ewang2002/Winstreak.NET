using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Winstreak.Extensions;
using Winstreak.WebApi.Hypixel.Definitions;
using static Winstreak.WebApi.ApiConstants;

namespace Winstreak.WebApi.Hypixel
{
	public class HypixelApi
	{
		public int MaximumRequestsInRateLimit = 16;
		public static TimeSpan HypixelRateLimit = TimeSpan.FromMinutes(1);

		private readonly CacheDictionary<string, HypixelPlayerApiResponse> _cache;
		private readonly string _apiKey;
		private readonly Timer _rateLimitTimer;
		private int _requestsMade;

		/// <summary>
		/// Hypixel API constructor.
		/// </summary>
		/// <param name="apiKey">The API key.</param>
		public HypixelApi(string apiKey)
		{
			_requestsMade = 0;
			_apiKey = apiKey;
			_rateLimitTimer = new Timer
			{
				Enabled = false,
				AutoReset = false,
				Interval = HypixelRateLimit.TotalMilliseconds
			};
			_cache = new CacheDictionary<string, HypixelPlayerApiResponse>();
		}

		/// <summary>
		/// Validates the API key.
		/// </summary>
		/// <returns>Whether the API key is valid or not.</returns>
		public async Task<bool> ValidateApiKeyAsync()
		{
			var resp = await SendRequestAsync<ValidateApiResponse>("key?");
			return resp.Success;
		}

		/// <summary>
		/// Sends a request to Hypixel.
		/// </summary>
		/// <typeparam name="T">The class to deserialize the JSON to.</typeparam>
		/// <param name="urlInfo">Any arguments; for example, "player?name=name</param>
		/// <returns>The .NET object corresponding to type "T".</returns>
		public async Task<T> SendRequestAsync<T>(string urlInfo)
		{
			if (_requestsMade + 1 > MaximumRequestsInRateLimit)
				throw new Exception("You have hit the rate limit.");

			using var reqMsgInfo = new HttpRequestMessage
			{
				RequestUri = new Uri($"https://api.hypixel.net/{urlInfo}&key={_apiKey}"),
				Method = HttpMethod.Get
			};

			using var resp = await ApiClient.SendAsync(reqMsgInfo);
			// start timer
			if (!_rateLimitTimer.Enabled)
			{
				_rateLimitTimer.Start();
				_rateLimitTimer.Elapsed += (sender, args) =>
				{
					_rateLimitTimer.Stop();
					_requestsMade = 0;
				};

				_requestsMade++;
			}
			else
				_requestsMade++;

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
		{
			if (_cache.Contains(name) && _cache[name].Success && _cache[name].Player != null)
			{
				_cache.ResetCacheTime(name);
				return _cache[name];
			}

			var data = await SendRequestAsync<HypixelPlayerApiResponse>($"player?name={name}&");
			_cache.TryAdd(name, data);
			return data;
		}

		/// <summary>
		/// Processes a list of names. 
		/// </summary>
		/// <param name="names">The names to look up.</param>
		/// <returns>A tuple containing three elements: one element consisting of all valid responses; another element consisting of all nicked players; a third element consisting of the names that couldn't be searched due to rate limit issues.</returns>
		public async Task<(IList<HypixelPlayerApiResponse> responses,
				IList<string> nicked,
				IList<string> unableToSearch)>
			ProcessListOfPlayers(IList<string> names)
		{
			var nicked = new List<string>();
			var unableToSearch = new List<string>();
			var responses = new List<HypixelPlayerApiResponse>();

			// names that wont error due to rate limit
			var actualNamesToLookUp = new List<string>();
			var tempReqMade = _requestsMade;
			foreach (var name in names)
			{
				// if in cache then
				// just use cached data
				if (_cache.Contains(name)
				    && _cache[name].Success
				    && _cache[name].Player != null)
				{
					responses.Add(_cache[name]);
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
					responses.Add(finishedReq);
				else
					nicked.Add(actualNamesToLookUp[i]);
			}

			return (responses, nicked, unableToSearch);
		}
	}
}