using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Winstreak.Cli.Configuration;
using Winstreak.Core.LogReader;
using Winstreak.Core.WebApi.Hypixel;

namespace Winstreak.Cli.DirectoryManager
{
	public static partial class DirectoryWatcher
	{
		public static string HelpInfo = new StringBuilder()
			.Append("Current Command List.")
			.AppendLine()
			.Append("> -clear OR -c: Clears the console.")
			.AppendLine()
			.Append(
				"> -tc: Determines whether the console should be cleared when a screenshot is provided.")
			.AppendLine()
			.Append("> -status: Views program status.")
			.AppendLine()
			.Append("> -config: Views current configuration.")
			.AppendLine()
			.Append("> -emptycache OR -clearcache: Empties the cache.")
			.AppendLine()
			.Append(
				"> -sortmode OR -sort OR -s: Changes the way the program sorts the presented data.")
			.AppendLine()
			.Append("> -help OR -h: Shows this menu.")
			.AppendLine()
			.Append("> -quit OR -q: Quits the program.")
			.AppendLine()
			.Append("> -stats OR -statistics: Shows basic statistics.")
			.AppendLine()
			.Append("\tOptional Argument:")
			.AppendLine()
			.Append("\t\t- s: Saves yours statistics to a file. Usage: -stats s")
			.ToString();

		public static readonly string Divider = "===================================";
		
		
		public static readonly string JoinedParty = " joined the party.";
		public static readonly string RemovedFromParty = " has been removed from the party.";
		public static readonly string YouLeftParty = "You left the party.";
		public static readonly string TheyLeftParty = " has left the party.";
		public static readonly string OnlinePrefix = "ONLINE: ";
		public static readonly string CantFindPlayer = "Can't find a player by the name of";
		public static readonly string CantFindPlayerAp = CantFindPlayer + " '";
		public static readonly string ApiKeyInfo = "Your new API key is ";
		public static readonly string DisbandParty = "has disbanded the party!";
		public static readonly string DisbandAlert = "The party was disbanded because all invited expired and the " +
		                                             "party was empty";

		public static readonly string YouPurchased = "You purchased ";

		// Configuration files
		public static ConfigFile Config;
		public static string[] ConfigRaw;
		public static DirectoryInfo McScreenshotsPath;

		// General properties
		public static int GuiScale;
		public static bool ShouldClearBeforeCheck;
		public static SortType SortingType = SortType.Score;

		// Things to keep in mind
		public static string[] NamesInExempt;
		public static MinecraftLogReader LogReader;
		// Keep in mind that the user can only be in one party at any given time.
		public static Dictionary<string, string> PartySession = new();
		public static Dictionary<string, int> ItemStatistics = new();
		public static DateTime StartedInstance = DateTime.Now;
		
		// API Key stuff
		public static HypixelApi HypixelApi;
		public static bool ApiKeyValid;
	}
}