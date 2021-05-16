using System.Collections.Generic;
using System.IO;

namespace Winstreak.Cli.Configuration
{
	public struct ConfigFile
	{
		public FileInfo FileData { get; set; }
		public string PathToMinecraftFolder { get; set; }
		public string PathToLogsFolder { get; set; }
		public List<string> ExemptPlayers { get; set; }
		public bool ClearConsole { get; set; }
		public int ScreenshotDelay { get; set; }
		public string HypixelApiKey { get; set; }
		public bool DeleteScreenshot { get; set; }
		public bool CheckFriends { get; set; }
		public bool SuppressErrorMessages { get; set; }
		public bool StrictParser { get; set; }
		public string YourIgn { get; set; }
	}
}