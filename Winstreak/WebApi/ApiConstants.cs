using System.Net.Http;
using Winstreak.WebApi.Definition;

namespace Winstreak.WebApi
{
	public static class ApiConstants
	{
		/// <summary>
		/// The cached data.
		/// </summary>
		public static CacheDictionary<string, BedwarsData> CachedData; 

		/// <summary>
		/// The API Client.
		/// </summary>
		public static HttpClient ApiClient;

		/// <summary>
		/// The constructor for this method.
		/// </summary>
		static ApiConstants()
		{
			ApiClient = new HttpClient();
			ApiClient.DefaultRequestHeaders.Add("User-Agent",
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36");
			CachedData = new CacheDictionary<string, BedwarsData>();
		}
	}
}