using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Winstreak.Request
{
	public class PlanckeApiRequester
	{
		private static readonly HttpClient Client = new HttpClient();
		public List<string> Names { get; }

		public PlanckeApiRequester(List<string> allNames)
		{
			Names = allNames;
		}

		public async Task<IDictionary<string, string>> SendRequests()
		{
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