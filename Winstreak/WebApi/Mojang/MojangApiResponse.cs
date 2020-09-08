using Newtonsoft.Json;

namespace Winstreak.WebApi.Mojang
{
	internal class MojangApiResponse
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("id")]
		public string Id { get; set; }
	}
}