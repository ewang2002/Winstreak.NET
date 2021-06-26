using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Winstreak.Cli.Configuration;
using Winstreak.Core.Logging;
using Winstreak.Core.Profile;
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
			.AppendLine()
			.Append("> -void OR -voiddeaths: Shows people that have died by falling into the void.")
			.ToString();

		public static readonly string Divider = "===================================";

		public const int UsernameMaxLen = 16;
		
		public const string JoinedParty = " joined the party.";
		public const string RemovedFromParty = " has been removed from the party.";
		public const string YouLeftParty = "You left the party.";
		public const string TheyLeftParty = " has left the party.";
		public const string OnlinePrefix = "ONLINE: ";
		public const string CantFindPlayer = "Can't find a player by the name of";
		public const string CantFindPlayerAp = CantFindPlayer + " '";
		public const string ApiKeyInfo = "Your new API key is ";
		public const string DisbandParty = "has disbanded the party!";
		public const string DisbandAlert = "The party was disbanded because all invited expired and the " +
		                                   "party was empty";

		public const string YouPurchased = "You purchased ";
		public const string FellIntoVoid = "fell into the void.";
		public const string FellIntoVoidFinal = "fell into the void. FINAL KILL!";

		// Configuration files
		public static ConfigFile Config;
		public static string[] ConfigRaw;
		public static PlayerProfile YourProfile;
		public static DirectoryInfo McScreenshotsPath;

		// General properties
		public static int GuiScale;
		public static bool ShouldClearBeforeCheck;
		public static SortType SortingType = SortType.Score;

		// Things to keep in mind
		public static string[] NamesInExempt;
		
		public static MinecraftLogReader LogReader;

#if DEBUG
		public static StreamWriter DebugLogger;
#endif

		// Keep in mind that the user can only be in one party at any given time.
		public static Dictionary<string, string> PartySession = new();
		public static Dictionary<string, int> ItemStatistics = new();
		public static Dictionary<string, int> VoidDeaths = new();
		public static DateTime StartedInstance = DateTime.Now;
		
		// API Key stuff
		public static HypixelApi HypixelApi;
		public static bool ApiKeyValid;
	}
}