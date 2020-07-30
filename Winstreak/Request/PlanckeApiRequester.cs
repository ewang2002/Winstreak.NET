using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Winstreak.Request
{
	public class PlanckeApiRequester
	{
		private static readonly HttpClient Client;
		public IList<string> Names { get; }

		static PlanckeApiRequester()
		{
			Client = new HttpClient();
			Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36");
		}

		public PlanckeApiRequester(IList<string> allNames)
		{
			Names = allNames;
		}

		public async Task<IDictionary<string, string>> SendRequests()
		{
			if (Client.DefaultRequestHeaders.Contains("X-Forwarded-For"))
			{
				Client.DefaultRequestHeaders.Remove("X-Forwarded-For");
			}
			Client.DefaultRequestHeaders.Add("X-Forwarded-For", GenerateRandomIpAddress());

			string[] urls = Names
				.Select(x => $"https://plancke.io/hypixel/player/stats/{x}")
				.ToArray();

			Task<HttpResponseMessage>[] requests = urls
				.Select(url => Client.GetAsync(url))
				.ToArray();

			await Task.WhenAll(requests);

			HttpResponseMessage[] responses = requests
				.Select(x => x.Result)
				.ToArray();

			IDictionary<string, string> returnVal = new Dictionary<string, string>();
			for (int i = 0; i < Names.Count; i++)
			{
				// does this even work?
				returnVal.Add(Names[i], await responses[i].Content.ReadAsStringAsync());
			}

			return returnVal;
		}

		private string GenerateRandomIpAddress()
		{
			Random r = new Random();
			return $"{r.Next(256)}.{r.Next(256)}.{r.Next(256)}.{r.Next(256)}";
		}
	}
}