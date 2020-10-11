using System.IO;
using System.Text;
using Winstreak.Parsers.ConfigParser;
using Winstreak.WebApi.Hypixel;

namespace Winstreak.DirectoryManager
{
	public static partial class DirectoryWatcher
	{
		public static string HelpInfo = new StringBuilder()
			.Append("[INFO] Current Command List.")
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
			.Append("> -emptycache: Empties the cache.")
			.AppendLine()
			.Append("> -gamemode OR -gm: Switches the parser gamemode from solos/doubles to 3s/4s or vice versa.")
			.AppendLine()
			.Append(
				"> -sortmode OR -sort OR -s: Changes the way the program sorts the presented data. By default, this is set to the Score value.")
			.AppendLine()
			.Append("> -help OR -h: Shows this menu.")
			.AppendLine()
			.Append("> -quit OR -q: Quits the program.")
			.ToString();

		public static readonly string Divider = "=====================================";

		public static DirectoryInfo McScreenshotsPath;

		public static int GuiScale;
		public static bool ShouldClearBeforeCheck;

		public static ConfigFile Config;
		public static int Mode = 34;

		public static HypixelApi HypixelApi;
		public static bool ApiKeyValid;

		public static SortType SortingType = SortType.Score;
	}
}