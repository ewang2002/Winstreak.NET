using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Winstreak.Request
{
	public class PlanckeApiRequester
	{
		public static readonly HttpClient Client;

		/// <summary>
		/// The list of names that were passed in from the constructor.
		/// </summary>
		public IList<string> Names { get; }

		/// <summary>
		/// Static constructor that instantiates the HttpClient.
		/// </summary>
		static PlanckeApiRequester()
		{
			Client = new HttpClient();
			Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36");
		}

		/// <summary>
		/// Creates a new PlanckeApiRequester object. Use this class to get raw stats for all names in the list (passed as parameter).
		/// </summary>
		/// <param name="allNames">The names of players to get stats for.</param>
		public PlanckeApiRequester(IList<string> allNames) => Names = allNames;
		
		/// <summary>
		/// Sends requests to Plancke for the stats of each name (of players) specified in the list passed in the constructor.
		/// </summary>
		/// <returns>A dictionary, with the key being the player's name and the value being the raw HTML data.</returns>
		public async Task<IDictionary<string, string>> SendRequests()
		{
			if (Client.DefaultRequestHeaders.Contains("X-Forwarded-For"))
				Client.DefaultRequestHeaders.Remove("X-Forwarded-For");

			Client.DefaultRequestHeaders.Add("X-Forwarded-For", GenerateRandomIpAddress());

			var urls = Names
				.Select(x => $"https://plancke.io/hypixel/player/stats/{x}")
				.ToArray();

			var requests = urls
				.Select(url => Client.GetAsync(url))
				.ToArray();

			await Task.WhenAll(requests);

			var responses = requests
				.Select(x => x.Result)
				.ToArray();

			var returnVal = new Dictionary<string, string>();
			for (var i = 0; i < Names.Count; i++)
			{
				// does this even work?
				returnVal.Add(Names[i], await responses[i].Content.ReadAsStringAsync());
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