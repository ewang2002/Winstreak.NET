﻿using System;
using System.IO;

namespace Winstreak
{
	public class Program
	{
		public static void Main(string[] args)
		{
			int brokenBeds;
			int finalKills;
			int amtTryHards;

			do
			{
				Console.WriteLine("How many broken beds does a tryhard have? ");
				brokenBeds = int.Parse(Console.ReadLine() ?? "250");
			} while (brokenBeds <= 0);

			do
			{
				Console.WriteLine("How many final kills does a tryhard have? ");
				finalKills = int.Parse(Console.ReadLine() ?? "750");
			} while (finalKills <= 0);

			do
			{
				Console.WriteLine("How many tryhards in lobby before we recommend you leave? ");
				amtTryHards = int.Parse(Console.ReadLine() ?? "250");
			} while (amtTryHards <= 0);

			Console.Clear();

			string path = Path.Join(@"C:\Users\ewang\AppData\Roaming\.minecraft\screenshots");
			Console.WriteLine("[INFO] Starting Service.");
			Console.WriteLine($"[INFO] Checking: {path}");
			Console.WriteLine("=========================");
		}
	}
}
