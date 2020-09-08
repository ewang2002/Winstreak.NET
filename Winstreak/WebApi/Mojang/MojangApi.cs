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
		/// Caching Names & UUID.
		/// </summary>
		public static ConcurrentDictionary<string, string> NameUuid = new ConcurrentDictionary<string, string>();

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