using System;
using System.Drawing;
using System.IO;
using System.Threading;
using Winstreak.Parser.V1;

namespace Winstreak.Directory
{
	public class DirectoryWatcher
	{
		public static void Run(string path, int finalKills, int brokenBeds)
		{
			using FileSystemWatcher watcher = new FileSystemWatcher
			{
				Path = path,
				// Only watch image files
				Filter = "*.png",
				// Filters
				NotifyFilter = NotifyFilters.FileName,
				// Add event handlers.
				// Begin watching.
				EnableRaisingEvents = true
			};
			watcher.Created += OnChanged;

			while (Console.ReadLine() != "q")
			{
			}
		}

		private static void OnChanged(object source, FileSystemEventArgs e)
		{
			Thread.Sleep(350);
			if (AbstractNameParser.IsInLobby(new Bitmap(e.FullPath)))
			{
				Console.WriteLine("Lobby");
			}
			else
			{
				Console.WriteLine("Game");
			}
		}
	}
}