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
			await DirectoryWatcher.Run(path);
		}
	}
}
