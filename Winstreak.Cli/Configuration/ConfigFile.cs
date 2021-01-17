namespace Winstreak.Cli.Configuration
{
	public struct ConfigFile
	{
		public string PathToMinecraftFolder { get; set; }
		public string[] ExemptPlayers { get; set; }
		public bool ClearConsole { get; set; }
		public int ScreenshotDelay { get; set; }
		public string HypixelApiKey { get; set; }
		public string[] DangerousPlayers { get; set; }
		public bool DeleteScreenshot { get; set; }
		public bool CheckFriends { get; set; }
		public bool SuppressErrorMessages { get; set; }
	}
}