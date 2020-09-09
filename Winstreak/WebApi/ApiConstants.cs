using System.Net.Http;

namespace Winstreak.WebApi
{
	public static class ApiConstants
	{
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
		}
	}
}