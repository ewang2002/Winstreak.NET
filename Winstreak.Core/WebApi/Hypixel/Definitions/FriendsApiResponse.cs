using Newtonsoft.Json;

namespace Winstreak.Core.WebApi.Hypixel.Definitions
{
	public class FriendsApiResponse
	{
		[JsonProperty("success")]
		public bool Success { get; set; }

		[JsonProperty("records")]
		public FriendsRecord[] Records { get; set; }
	}

	public class FriendsRecord
	{
		[JsonProperty("_id")]
		public string Id { get; set; }

		[JsonProperty("uuidSender")]
		public string UuidSender { get; set; }

		[JsonProperty("uuidReceiver")]
		public string UuidReceiver { get; set; }

		[JsonProperty("started")]
		public long Started { get; set; }
	}
}