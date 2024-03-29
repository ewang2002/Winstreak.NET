﻿using Newtonsoft.Json;

namespace Winstreak.Core.WebApi.Mojang.Definitions
{
	public class UuidToNameResponse
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("changedToAt", NullValueHandling = NullValueHandling.Ignore)]
		public long? ChangedToAt { get; set; }
	}
}