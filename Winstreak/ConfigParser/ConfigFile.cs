namespace Winstreak.ConfigParser
{
	public struct ConfigFile
	{
		public string PathToMinecraftFolder;
		public string[] ExemptPlayers;
		public int RetryDelay;
		public int RetryMax;
		public bool ClearConsole;
		public int ScreenshotDelay;
	}
}