using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Winstreak.Dir;

namespace Winstreak
{
	public class Program
	{
		public static async Task Main()
		{
			int brokenBeds;
			int finalKills;
			int amtTryHards;

			do
			{
				Console.WriteLine("How many broken beds does a tryhard have? ");
				try
				{
					brokenBeds = int.Parse(Console.ReadLine() ?? "250");
				}
				catch (Exception)
				{
					brokenBeds = 300; 
				}
			} while (brokenBeds <= 0);

			do
			{
				Console.WriteLine("How many final kills does a tryhard have? ");
				try
				{
					finalKills = int.Parse(Console.ReadLine() ?? "750");
				}
				catch (Exception)
				{
					finalKills = 850; 
				}
			} while (finalKills <= 0);

			do
			{
				Console.WriteLine("How many tryhards in lobby before we recommend you leave? ");
				try
				{
					amtTryHards = int.Parse(Console.ReadLine() ?? "250");
				}
				catch (Exception)
				{
					amtTryHards = 4;
				}
			} while (amtTryHards <= 0);

			Console.Clear();
			string path;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				path = Path.Join("C:", "Users", Environment.UserName, "AppData", "Roaming", ".minecraft");
			}
			else
			{
				Console.WriteLine("Please copy and paste the Minecraft folder here. The folder should end with \".minecraft.\"");
				path = Console.ReadLine();
			}

			if (!Directory.Exists(path))
			{
				Console.WriteLine($"[ERROR] Your Minecraft folder wasn't found. Please type the path to your folder (ends with \".minecraft.\"");
				path = Console.ReadLine();
			}

			Console.WriteLine("[INFO] Starting Service.");
			Console.WriteLine($"[INFO] Checking: {Path.Join(path, "screenshots")}");
			await DirectoryWatcher.Run(path, finalKills, brokenBeds, amtTryHards);
		}
	}
}
