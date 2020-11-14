namespace Winstreak.Cli.Configuration
{
	public struct ConfigFile
	{
		public string PathToMinecraftFolder;
		public string[] ExemptPlayers;
		public bool ClearConsole;
		public int ScreenshotDelay;
		public string HypixelApiKey;
		public string[] DangerousPlayers;
		public bool DeleteScreenshot;
		public bool CheckFriends;
	}
}