using Newtonsoft.Json;

namespace Winstreak.Core.WebApi.Hypixel.Definitions
{
	public class GuildsApiResponse
	{
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("guild")]
        public Guild Guild { get; set; }
    }

    public class Guild
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("members")]
        public Member[] Members { get; set; }

        [JsonProperty("exp")]
        public long Exp { get; set; }

        [JsonProperty("name_lower")]
        public string NameLower { get; set; }

        [JsonProperty("preferredGames")]
        public string[] PreferredGames { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }
    }

    public class Member
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("rank")]
        public string Rank { get; set; }

        [JsonProperty("joined")]
        public long Joined { get; set; }
    }
}