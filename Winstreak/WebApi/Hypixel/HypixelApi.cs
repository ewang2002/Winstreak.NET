using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Winstreak.WebApi.Hypixel.Definitions;
using static Winstreak.WebApi.ApiConstants;

namespace Winstreak.WebApi.Hypixel
{
	public class HypixelApi
	{
		public int MaximumRequestsInRateLimit = 120;
		public static TimeSpan HypixelRateLimit = TimeSpan.FromMinutes(1);

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
				Interval = HypixelRateLimit.Milliseconds
			};
		}

		/// <summary>
		/// Validates the API key.
		/// </summary>
		/// <returns>Whether the API key is valid or not.</returns>
		public async Task<bool> ValidateApiKeyAsync()
		{
			var resp = await SendRequestAsync<ValidateApiResponse>("key");
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

			resp.EnsureSuccessStatusCode();

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
		public async Task<HypixelPlayerApiResponse> GetPlayerInfo(string name)
		{
			var data = await SendRequestAsync<HypixelPlayerApiResponse>($"player?name={name}");

			return data;
		}

		/// <summary>
		/// Processes a list of names. 
		/// </summary>
		/// <param name="names">The names to look up.</param>
		/// <returns>A tuple containing three elements: one element consisting of all valid responses; another element consisting of all nicked players; and a third element consisting of the names that couldn't be searched due to rate limit issues.</returns>
		public async Task<(IList<HypixelPlayerApiResponse> responses,
				IList<string> nicked,
				IList<string> unableToSearch)>
			ProcessListOfPlayers(IList<string> names)
		{
			
		}
	}
}