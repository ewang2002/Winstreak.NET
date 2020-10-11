﻿using Newtonsoft.Json;

namespace Winstreak.WebApi.Mojang.Definitions
{
	internal class NameToUuidResponse
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("id")]
		public string Id { get; set; }
	}
}