﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Winstreak.WebApi.Definition;
using Winstreak.WebApi.Hypixel.Definitions;
using static Winstreak.WebApi.ApiConstants;

namespace Winstreak.WebApi.Hypixel
{
	public class HypixelApi
	{
		public int MaximumRequestsInRateLimit = 120;
		public static TimeSpan HypixelRateLimit = TimeSpan.FromMinutes(1);

		private readonly string _apiKey;

		/// <summary>
		/// The timer.
		/// </summary>
		public Timer RateLimitTimer { get; set; }

		/// <summary>
		/// The number of requests made within the time period.
		/// </summary>
		public int RequestsMade { get; set; }

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

				RequestsMade++;
			}
			else
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
		/// Processes a list of names. 
		/// </summary>
		/// <param name="names">The names to look up.</param>
		/// <returns>A tuple containing three elements: one element consisting of all valid responses; another element consisting of all nicked players; a third element consisting of the names that couldn't be searched due to rate limit issues.</returns>
		public async Task<(IList<BedwarsData> responses,
				IList<string> nicked,
				IList<string> unableToSearch)>
			ProcessListOfPlayers(IList<string> names)
		{
			var nicked = new List<string>();
			var unableToSearch = new List<string>();
			var responses = new List<BedwarsData>();

			// names that wont error due to rate limit
			var actualNamesToLookUp = new List<string>();
			var tempReqMade = RequestsMade;
			foreach (var name in names)
			{
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
					responses.Add(new BedwarsData(finishedReq));
				else
					nicked.Add(actualNamesToLookUp[i]);
			}

			return (responses, nicked, unableToSearch);
		}
	}
}