using System;
using Newtonsoft.Json;

namespace Winstreak.WebApi.Hypixel.Definitions
{
	public class ValidateApiResponse
	{
		[JsonProperty("success")]
		public bool Success { get; set; }

#nullable enable
		[JsonProperty("record")]
		public ApiRespRecord? Record { get; set; }
#nullable disable
	}

	public class ApiRespRecord
	{
		[JsonProperty("key")]
		public Guid Key { get; set; }

		[JsonProperty("owner")]
		public Guid Owner { get; set; }

		[JsonProperty("limit")]
		public long Limit { get; set; }

		[JsonProperty("queriesInPastMin")]
		public long QueriesInPastMin { get; set; }

		[JsonProperty("totalQueries")]
		public long TotalQueries { get; set; }
	}
}
