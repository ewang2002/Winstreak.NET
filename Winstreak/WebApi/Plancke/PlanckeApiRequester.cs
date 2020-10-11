using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Winstreak.DirectoryManager;
using static Winstreak.WebApi.ApiConstants;

namespace Winstreak.WebApi.Plancke
{
	public class PlanckeApiRequester
	{
		/// <summary>
		/// The list of names that were passed in from the constructor.
		/// </summary>
		public IList<string> Names { get; }

		/// <summary>
		/// Creates a new PlanckeApiRequester object. Use this class to get raw stats for all names in the list (passed as parameter).
		/// </summary>
		/// <param name="allNames">The names of players to get stats for.</param>
		public PlanckeApiRequester(IList<string> allNames) => Names = allNames;

		/// <summary>
		/// Sends requests to Plancke for the stats of each name (of players) specified in the list passed in the constructor.
		/// </summary>
		/// <returns>A dictionary, with the key being the player's name and the value being the raw HTML data.</returns>
		public async Task<IDictionary<string, string>> SendRequestsAsync()
		{
			if (ApiClient.DefaultRequestHeaders.Contains("X-Forwarded-For"))
				ApiClient.DefaultRequestHeaders.Remove("X-Forwarded-For");

			ApiClient.DefaultRequestHeaders.Add("X-Forwarded-For", GenerateRandomIpAddress());

			var urls = Names
				.Select(x => $"https://plancke.io/hypixel/player/stats/{x}")
				.ToArray();

			var requests = urls
				.Select(url => ApiClient.GetAsync(url))
				.ToArray();

			await Task.WhenAll(requests);

			var responses = requests
				.Select(x => x.Result)
				.ToArray();

			var returnVal = new Dictionary<string, string>();
			for (var i = 0; i < Names.Count; i++)
			{
				if (responses[i].StatusCode != HttpStatusCode.OK)
				{
					// in case not found.
					for (var attempts = 0; attempts < DirectoryWatcher.Config.RetryMax; attempts++)
					{
						await Task.Delay(TimeSpan.FromMilliseconds(DirectoryWatcher.Config.RetryDelay));
						// get it again
						responses[i] = await ApiClient
							.GetAsync($"https://plancke.io/hypixel/player/stats/{Names[i]}");
						if (responses[i].StatusCode == HttpStatusCode.OK)
							break;
					}
				}

				var msg = await responses[i].Content.ReadAsStringAsync();
				returnVal.Add(Names[i], msg);
			}

			return returnVal;
		}

		/// <summary>
		/// Generates a random IP address.
		/// </summary>
		/// <returns>A random IP address.</returns>
		private string GenerateRandomIpAddress()
		{
			var r = new Random();
			return $"{r.Next(256)}.{r.Next(256)}.{r.Next(256)}.{r.Next(256)}";
		}
	}
}