using System.Collections.Generic;
using Newtonsoft.Json;

namespace Winstreak.WebApi.Hypixel.Definitions
{
	public class HypixelPlayerApiResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

#nullable enable
	    [JsonProperty("player")]
        public Player? Player { get; set; }
#nullable disable
	}

    public class Player
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("firstLogin")]
        public long FirstLogin { get; set; }

        [JsonProperty("playername")]
        public string PlayerName { get; set; }

        [JsonProperty("lastLogin")]
        public long LastLogin { get; set; }

        [JsonProperty("displayname")]
        public string DisplayName { get; set; }

        [JsonProperty("knownAliases")]
        public string[] KnownAliases { get; set; }

        [JsonProperty("knownAliasesLower")]
        public string[] KnownAliasesLower { get; set; }

        [JsonProperty("achievementsOneTime")]
        public string[] AchievementsOneTime { get; set; }

        [JsonProperty("stats")]
        public Stats Stats { get; set; }

        [JsonProperty("achievementPoints")]
        public long AchievementPoints { get; set; }

        [JsonProperty("achievements")]
        public Dictionary<string, long> Achievements { get; set; }

        [JsonProperty("lastLogout")]
        public long LastLogout { get; set; }

        [JsonProperty("achievementTracking")]
        public object[] AchievementTracking { get; set; }

        [JsonProperty("networkExp")]
        public long NetworkExp { get; set; }

        [JsonProperty("karma")]
        public long Karma { get; set; }
    }

    public class Stats
    {
	    [JsonProperty("Bedwars")]
        public Bedwars Bedwars { get; set; }
    }

    public class Bedwars
    {
	    [JsonProperty("eight_one_kills_bedwars")]
	    public long SolosKills;

	    [JsonProperty("eight_one_deaths_bedwars")]
	    public long SolosDeaths;

	    [JsonProperty("eight_one_final_kills_bedwars")]
	    public long SolosFinalKills;

	    [JsonProperty("eight_one_final_deaths_bedwars")]
	    public long SolosFinalDeaths;

	    [JsonProperty("eight_one_wins_bedwars")]
	    public long SolosWins;

	    [JsonProperty("eight_one_losses_bedwars")]
	    public long SolosLosses;

	    [JsonProperty("eight_one_beds_broken_bedwars")]
	    public long SolosBrokenBeds;

	    [JsonProperty("eight_two_kills_bedwars")]
	    public long DoublesKills;

	    [JsonProperty("eight_two_deaths_bedwars")]
	    public long DoublesDeaths;

	    [JsonProperty("eight_two_final_kills_bedwars")]
	    public long DoublesFinalKills;

	    [JsonProperty("eight_two_final_deaths_bedwars")]
	    public long DoublesFinalDeaths;

	    [JsonProperty("eight_two_wins_bedwars")]
	    public long DoublesWins;

	    [JsonProperty("eight_two_losses_bedwars")]
	    public long DoublesLosses;

	    [JsonProperty("eight_two_beds_broken_bedwars")]
	    public long DoublesBrokenBeds;

	    [JsonProperty("four_three_kills_bedwars")]
	    public long ThreesKills;

	    [JsonProperty("four_three_deaths_bedwars")]
	    public long ThreesDeaths;

	    [JsonProperty("four_three_final_kills_bedwars")]
	    public long ThreesFinalKills;

	    [JsonProperty("four_three_final_deaths_bedwars")]
	    public long ThreesFinalDeaths;

	    [JsonProperty("four_three_wins_bedwars")]
	    public long ThreesWins;

	    [JsonProperty("four_three_losses_bedwars")]
	    public long ThreesLosses;

	    [JsonProperty("four_three_beds_broken_bedwars")]
	    public long ThreesBrokenBeds;

	    [JsonProperty("four_four_kills_bedwars")]
	    public long FoursKills;

	    [JsonProperty("four_four_deaths_bedwars")]
	    public long FoursDeaths;

	    [JsonProperty("four_four_final_kills_bedwars")]
	    public long FoursFinalKills;

	    [JsonProperty("four_four_final_deaths_bedwars")]
	    public long FoursFinalDeaths;

	    [JsonProperty("four_four_wins_bedwars")]
	    public long FoursWins;

	    [JsonProperty("four_four_losses_bedwars")]
	    public long FoursLosses;

	    [JsonProperty("four_four_beds_broken_bedwars")]
	    public long FoursBrokenBeds;
	}
}
