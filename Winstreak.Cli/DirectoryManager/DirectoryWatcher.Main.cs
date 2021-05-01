#define USE_NEW_PARSER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Winstreak.Cli.Configuration;
using Winstreak.Cli.Utility;
using Winstreak.Cli.Utility.ConsoleTable;
using Winstreak.Core.Extensions;
using Winstreak.Core.LogReader;
using Winstreak.Core.Parsers.ImageParser;
using Winstreak.Core.Parsers.ImageParser.Imaging;
using static Winstreak.Core.WebApi.CachedData;
using Winstreak.Core.WebApi.Hypixel;
using Winstreak.Core.WebApi.Plancke;

namespace Winstreak.Cli.DirectoryManager
{
	public static partial class DirectoryWatcher
	{
		/// <summary>
		/// The "main" entry point for the program. This is where the program will be executing from.
		/// </summary>
		/// <param name="file">The configuration file.</param>
		/// <returns>Nothing.</returns>
		[SuppressMessage("Microsoft.Style", "IDE0042")]
		public static async Task RunAsync(ConfigFile file)
		{
			// init vars
			Config = file;
			var pathToScreenshots = Path.Join(Config.PathToMinecraftFolder, "screenshots");
			McScreenshotsPath = Directory.Exists(pathToScreenshots)
				? new DirectoryInfo(Path.Join(Config.PathToMinecraftFolder, "screenshots"))
				: Directory.CreateDirectory(pathToScreenshots);
			ShouldClearBeforeCheck = file.ClearConsole;

			// Get gui scale
			GuiScale = ParserHelper.GetGuiScale(Config.PathToMinecraftFolder);

			if (GuiScale == 0)
			{
				OutputDisplayer.WriteLine(LogType.Error, "Please set a non-automatic " +
				                                         "GUI scale in your Minecraft settings " +
				                                         "and then restart the program.");
				return;
			}

			var version = Assembly.GetEntryAssembly()?.GetName().Version;
			Console.WriteLine("%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%");
			Console.WriteLine($"Winstreak For Hypixel Bedwars");
			if (version != null)
				Console.WriteLine($"Version: {version}");
			Console.WriteLine("By CM19 & icicl");
#if DEBUG
			Console.WriteLine($"{AnsiConstants.TextBrightRedAnsi}Debug Mode!{AnsiConstants.ResetAnsi}");
#endif
			Console.WriteLine("%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%=%");
			
			OutputDisplayer.WriteLine(LogType.Info, "Attempting to connect to Hypixel's API...");
			var res = await ValidateApiKey(file.HypixelApiKey);
			OutputDisplayer.WriteLine(LogType.Info, res 
				? "Connected to Hypixel's API." 
				: "Unable to connect to Hypixel's API. Using Plancke.");
			OutputDisplayer.WriteLine(LogType.Info, "Ready.");
			Console.WriteLine(Divider);

			OutputDisplayer.WriteLine(LogType.Info, $"Minecraft Folder Set: {Config.PathToMinecraftFolder}");
			OutputDisplayer.WriteLine(LogType.Info, $"Logs Folder Set: {Config.PathToLogsFolder}");
			OutputDisplayer.WriteLine(LogType.Info, $"{Config.ExemptPlayers.Count} Exempt Players Set.");
			Console.WriteLine();
			OutputDisplayer.WriteLine(LogType.Info, "To use, simply take a screenshot in Minecraft by pressing F2.");
			OutputDisplayer.WriteLine(LogType.Info, "Need help? Type -h in here!");
			OutputDisplayer.WriteLine(LogType.Info, "To view current configuration, type -config in here!");
			Console.WriteLine(Divider);

			// make all lowercase for ease of comparison 
			Config.ExemptPlayers = Config.ExemptPlayers
				.Select(x => x.ToLower())
				.ToList();
			NamesInExempt = Config.ExemptPlayers.ToArray();

			// init watcher
			using var watcher = new FileSystemWatcher
			{
				Path = McScreenshotsPath.FullName,
				// Only watch image files
				Filter = "*.png",
				// Filters
				NotifyFilter = NotifyFilters.FileName,
				// Add event handlers.
				// Begin watching.
				EnableRaisingEvents = true
			};
			watcher.Created += OnChangedAsync;

			// Log time
			LogReader = new MinecraftLogReader(Config.PathToLogsFolder);
			LogReader.OnLogUpdate += LogUpdate;
			LogReader.Start();

			// infinite loop for command processing
			while (true)
			{
				var input = (Console.ReadLine() ?? string.Empty).Trim();
				if (input == string.Empty)
					continue;

				if (input.StartsWith('-'))
				{
					// quit program
					if (input.ToLower() == "-q" || input.ToLower() == "-quit")
						break;

					switch (input.ToLower().Trim())
					{
						case "-config":
							var configTable = new Table(2)
								.AddRow("Name", "Value")
								.AddSeparator()
								.AddRow("MC Folder", Config.PathToMinecraftFolder)
								.AddRow("Logs Folder", Config.PathToLogsFolder)
								.AddRow("API Key Valid?", ApiKeyValid)
								.AddRow("Delete Screenshots?", file.DeleteScreenshot)
								.AddRow("Checking Friends?", ApiKeyValid && file.CheckFriends)
								.AddRow("Suppress Errors?", Config.SuppressErrorMessages)
								.AddRow("Screenshot Delay?", Config.SuppressErrorMessages)
								.AddRow("GUI Scale", GuiScale)
								.AddRow("Strict Parser?", Config.StrictParser);
							Console.WriteLine(configTable.ToString());
							Console.WriteLine($"Exempt Players: {Config.ExemptPlayers.ToReadableString()}");
							Console.WriteLine(Divider);
							continue;
						case "-help":
						case "-h":
							OutputDisplayer.WriteLine(LogType.Info, HelpInfo);
							Console.WriteLine(Divider);
							continue;
						case "-clear":
						case "-c":
							Console.Clear();
							continue;
						case "-tc":
							ShouldClearBeforeCheck = !ShouldClearBeforeCheck;
							OutputDisplayer.WriteLine(LogType.Info, ShouldClearBeforeCheck
								? "Console will be cleared once a screenshot is provided."
								: "Console will not be cleared once a screenshot is provided.");
							Console.WriteLine(Divider);
							continue;
						case "-status":
							var valid = HypixelApi != null && ApiKeyValid;
							var usage = valid
								? $"{HypixelApi.RequestsMade}/{HypixelApi.MaximumRequestsInRateLimit}"
								: "Unlimited (Plancke)";
							var statusTable = new Table(2)
								.AddRow("Name", "Value")
								.AddSeparator()
								.AddRow("API Key Valid?", valid)
								.AddRow("Usage", usage)
								.AddRow("Player Cache Count", CachedPlayerData.Length)
								.AddRow("Friend Cache Count", CachedFriendsData.Length)
								.AddRow("Sort Mode", SortingType);
							Console.WriteLine(statusTable.ToString());
							Console.WriteLine(Divider);
							continue;
						case "-clearcache":
						case "-emptycache":
							OutputDisplayer.WriteLine(LogType.Info, "Cache has been cleared.");
							CachedPlayerData.Empty();
							CachedFriendsData.Empty();
							CachedGuildData.Empty();
							Console.WriteLine(Divider);
							continue;
						case "-sortmode":
						case "-sort":
						case "-s":
							SortingType = SortingType switch
							{
								SortType.Beds => SortType.Finals,
								SortType.Finals => SortType.Fkdr,
								SortType.Fkdr => SortType.Winstreak,
								SortType.Winstreak => SortType.Level,
								_ => SortType.Fkdr
							};

							OutputDisplayer.WriteLine(LogType.Info, $"Sorting By: {SortingType}");
							Console.WriteLine(Divider);
							continue;
						case "-party":
							OutputDisplayer.WriteLine(LogType.Info, $"[INFO] {PartySession.Count} Party Members");
							foreach (var (lowercase, member) in PartySession)
								Console.WriteLine($"\t- {member} ({lowercase})");
							Console.WriteLine(Divider);
							continue;
					}

					OutputDisplayer.WriteLine(LogType.Info, HelpInfo);
					Console.WriteLine(Divider);
					continue;
				}

				if (input.Contains('-') || input.Contains('\\'))
					continue;

				var ignsToCheck = input.Split(" ")
					.Select(x => x.Trim())
					.Where(x => x != string.Empty)
					.ToList();

				// check ign
				var checkTime = new Stopwatch();
				checkTime.Start();
				var (profiles, nicked) = await PlanckeApi
					.GetMultipleProfilesFromPlanckeAsync(ignsToCheck);

				if (profiles.Count == 1)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"[{profiles[0].BedwarsLevel}] {profiles[0].Name}");
					Console.ResetColor();

					var eightOne = profiles[0].EightOneBedwarsStats;
					var eightOneKdr = eightOne.GetKdr();
					var eightOneFkdr = eightOne.GetFkdr();
					var eightOneWlr = eightOne.GetWinLossRatio();

					var eightTwo = profiles[0].EightTwoBedwarsStats;
					var eightTwoKdr = eightTwo.GetKdr();
					var eightTwoFkdr = eightTwo.GetFkdr();
					var eightTwoWlr = eightTwo.GetWinLossRatio();

					var fourThree = profiles[0].FourThreeBedwarsStats;
					var fourThreeKdr = fourThree.GetKdr();
					var fourThreeFkdr = fourThree.GetFkdr();
					var fourThreeWlr = fourThree.GetWinLossRatio();

					var fourFour = profiles[0].FourFourBedwarsStats;
					var fourFourKdr = fourFour.GetKdr();
					var fourFourFkdr = fourFour.GetFkdr();
					var fourFourWlr = fourFour.GetWinLossRatio();

					var overall = profiles[0].OverallBedwarsStats;
					var overallKdr = overall.GetKdr();
					var overallFkdr = overall.GetFkdr();
					var overallWlr = overall.GetWinLossRatio();

					var table = new Table(11)
						.AddRow("Type", "Kills", "Deaths", "KDR", "F Kills", "F Deaths", "FKDR", "Wins",
							"Losses", "WLR", "Beds")
						.AddSeparator()
						.AddRow("Solos", eightOne.Kills, eightOne.Deaths,
							eightOneKdr.dZero
								? "-"
								: Math.Round(eightOneKdr.kdr, 2) + "",
							eightOne.FinalKills, eightOne.FinalDeaths,
							eightOneFkdr.fdZero
								? "-"
								: Math.Round(eightOneFkdr.fkdr, 2) + "",
							eightOne.Wins, eightOne.Losses,
							eightOneWlr.lZero
								? "-"
								: Math.Round(eightOneWlr.wlr, 2) + "",
							eightOne.BrokenBeds)
						.AddSeparator()
						.AddRow("Doubles", eightTwo.Kills, eightTwo.Deaths,
							eightTwoKdr.dZero
								? "-"
								: Math.Round(eightTwoKdr.kdr, 2) + "",
							eightTwo.FinalKills, eightTwo.FinalDeaths,
							eightTwoFkdr.fdZero
								? "-"
								: Math.Round(eightTwoFkdr.fkdr, 2) + "",
							eightTwo.Wins, eightTwo.Losses,
							eightTwoWlr.lZero
								? "-"
								: Math.Round(eightTwoWlr.wlr, 2) + "",
							eightTwo.BrokenBeds)
						.AddSeparator()
						.AddRow("3v3v3v3", fourThree.Kills, fourThree.Deaths,
							fourThreeKdr.dZero
								? "-"
								: Math.Round(fourThreeKdr.kdr, 2) + "",
							fourThree.FinalKills, fourThree.FinalDeaths,
							fourThreeFkdr.fdZero
								? "-"
								: Math.Round(fourThreeFkdr.fkdr, 2) + "",
							fourThree.Wins, fourThree.Losses,
							fourThreeWlr.lZero
								? "-"
								: Math.Round(fourThreeWlr.wlr, 2) + "",
							fourThree.BrokenBeds)
						.AddSeparator()
						.AddRow("4v4v4v4", fourFour.Kills, fourFour.Deaths,
							fourFourKdr.dZero
								? "-"
								: Math.Round(fourFourKdr.kdr, 2) + "",
							fourFour.FinalKills, fourFour.FinalDeaths,
							fourFourFkdr.fdZero
								? "-"
								: Math.Round(fourFourFkdr.fkdr, 2) + "",
							fourFour.Wins, fourFour.Losses,
							fourFourWlr.lZero
								? "-"
								: Math.Round(fourFourWlr.wlr, 2) + "",
							fourFour.BrokenBeds)
						.AddSeparator()
						.AddRow("Overall", overall.Kills, overall.Deaths,
							overallKdr.dZero
								? "-"
								: Math.Round(overallKdr.kdr, 2) + "",
							overall.FinalKills, overall.FinalDeaths,
							overallFkdr.fdZero
								? "-"
								: Math.Round(overallFkdr.fkdr, 2) + "",
							overall.Wins, overall.Losses,
							overallWlr.lZero
								? "-"
								: Math.Round(overallWlr.wlr, 2) + "",
							overall.BrokenBeds);

					Console.WriteLine(table.ToString());
					Console.WriteLine($"> Winstreak: {profiles[0].Winstreak}");
					Console.WriteLine($"> Network Level: {profiles[0].NetworkLevel}");
					Console.WriteLine($"> Karma: {profiles[0].Karma}");
					Console.WriteLine($"> First Joined: {profiles[0].FirstJoined:MM/dd/yyyy hh:mm tt}");
				}
				else
				{
					var table = new Table(6)
						.AddRow("LVL", "Username", "FKDR", "Beds", "W/L", "WS")
						.AddSeparator();
					foreach (var bedwarsData in profiles)
					{
						table.AddRow(
							bedwarsData.BedwarsLevel,
							bedwarsData.Name,
							bedwarsData.OverallBedwarsStats.FinalDeaths == 0
								? "N/A"
								: Math.Round(
										(double) bedwarsData.OverallBedwarsStats.FinalKills /
										bedwarsData.OverallBedwarsStats.FinalDeaths, 2)
									.ToString(CultureInfo.InvariantCulture),
							bedwarsData.OverallBedwarsStats.BrokenBeds,
							bedwarsData.OverallBedwarsStats.Losses == 0
								? "N/A"
								: Math.Round(
										(double) bedwarsData.OverallBedwarsStats.Wins /
										bedwarsData.OverallBedwarsStats.Losses, 2)
									.ToString(CultureInfo.InvariantCulture),
							bedwarsData.Winstreak
						);
					}

					if (nicked.Count > 0)
					{
						if (profiles.Count > 0)
							table.AddSeparator();

						foreach (var erroredPlayer in nicked)
							table.AddRow(
								"N/A",
								erroredPlayer,
								"N/A",
								"N/A",
								"N/A",
								"N/A"
							);
					}

					Console.WriteLine(table.ToString());
				}

				checkTime.Stop();
				Console.WriteLine();
				Console.WriteLine($"> Time Taken: {checkTime.Elapsed.TotalSeconds} Seconds.");
				Console.WriteLine(Divider);
			}
		}

		/// <summary>
		/// The method that is responsible for interpreting the Minecraft log messages.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="text">The text.</param>
		private static async void LogUpdate(object source, string text)
		{
			// Determine if the message is legit.
			if (!IsValidLogMessage(text, out var logImp))
				return;

			// Handle various cases.
			// Joined the party
			if (!logImp.Contains(":") && logImp.Contains(JoinedParty))
			{
				var name = logImp
					.Replace("-----------------------------", string.Empty)
					.Trim()
					.Split(JoinedParty)[0]
					.Trim();
				if (name[0] == '[')
					name = name.Split(']')[1].Trim();
				Console.WriteLine($"[INFO] {name} has joined the party.");

				if (!PartySession.ContainsKey(name.ToLower()))
					PartySession.Add(name.ToLower(), name);

				if (!Config.ExemptPlayers.Contains(name.ToLower()))
				{
					Config.ExemptPlayers.Add(name.ToLower());
					Console.WriteLine($"[INFO] \"{name}\" has been added to your exempt list.");
				}

				Console.WriteLine(Divider);
				return;
			}

			// Removed from party.
			if (!logImp.Contains(":") && logImp.Contains(RemovedFromParty))
			{
				var name = logImp
					.Replace("-----------------------------", string.Empty)
					.Trim()
					.Split(RemovedFromParty)[0]
					.Trim();
				if (name[0] == '[')
					name = name.Split(']')[1].Trim();
				Console.WriteLine($"[INFO] {name} has been removed from the party.");

				PartySession.Remove(name.ToLower());
				if (NamesInExempt.Any(x => string.Equals(x, name, StringComparison.CurrentCultureIgnoreCase)))
				{
					Console.WriteLine(Divider);
					return;
				}

				Config.ExemptPlayers.Remove(name.ToLower());
				Console.WriteLine($"[INFO] \"{name}\" has been removed from your exempt list.");
				Console.WriteLine(Divider);
				return;
			}

			// You left the party.
			if (!logImp.Contains(":") && (logImp.Contains(YouLeftParty) || logImp.Contains(DisbandParty)))
			{
				Console.WriteLine("[INFO] You left your current party!");
				foreach (var (lowerName, name) in PartySession)
				{
					if (NamesInExempt.Contains(lowerName))
						continue;

					Config.ExemptPlayers.Remove(lowerName);
					Console.WriteLine($"\t- \"{name}\" has been removed from your exempt list.");
				}

				PartySession.Clear();
				Console.WriteLine(Divider);
				return;
			}
			
			// Left the party.
			if (!logImp.Contains(":") && logImp.Contains(TheyLeftParty))
			{
				var name = logImp
					.Replace("-----------------------------", string.Empty)
					.Trim()
					.Split(TheyLeftParty)[0]
					.Trim();
				if (name[0] == '[')
					name = name.Split(']')[1].Trim();
				Console.WriteLine($"[INFO] {name} has left the party!");

				if (NamesInExempt.Any(x => string.Equals(x, name, StringComparison.CurrentCultureIgnoreCase)))
				{
					Console.WriteLine(Divider);
					return;
				}
				
				PartySession.Remove(name.ToLower());
				Config.ExemptPlayers.Remove(name.ToLower());
				Console.WriteLine($"[INFO] \"{name}\" has been removed from your exempt list.");
				Console.WriteLine(Divider);
				return;
			}
			
			// Disband
			if (!logImp.Contains(':') && logImp.Contains(DisbandAlert))
			{
				Console.WriteLine("[INFO] The party was disbanded!");
				foreach (var (lowerName, name) in PartySession)
				{
					if (NamesInExempt.Contains(lowerName))
						continue;

					Config.ExemptPlayers.Remove(lowerName);
					Console.WriteLine($"\t- \"{name}\" has been removed from your exempt list.");
				}
				
				PartySession.Clear();
				return;
			}

			// /who command used.
			var idxOfComma = text.IndexOf("ONLINE: ", StringComparison.Ordinal);
			if (logImp.Count(x => x == ':') == 1
			    && logImp.Contains(OnlinePrefix)
			    && logImp[idxOfComma..].Contains(','))
			{
				var names = logImp.Split(OnlinePrefix)[1]
					.Split(", ")
					.Select(x => x.Trim())
					.Where(x => x.Length > 0)
					.Where(name => !Config.ExemptPlayers.Contains(name.ToLower()))
					.ToList();

				if (names.Count == 0) return;
				Console.WriteLine("[INFO] Received /who Command Output.");
				await ProcessLobbyScreenshotAsync(names, TimeSpan.FromMinutes(0));
				return;
			}

			// /p list used.
			// Guaranteed to have a party leader.
			if (logImp.Contains("Party Leader") && logImp.Contains("Party Members (")
			                                    && logImp[..30].Trim() == "-----------------------------")
			{
				Console.WriteLine("[INFO] Party List Output Received.");
				var allPeople = logImp.Split(Environment.NewLine)
					.Where(x => x != "-----------------------------")
					.Where(x => (x.Contains("Party Leader")
					             || x.Contains("Party Moderator")
					             || x.Contains("Party Members")) && !x.Contains("Party Members ("))
					.SelectMany(x => x.Split(":")[^1].Trim()
						.Split("?")
						.Select(y => y.Trim())
						.Where(z => z.Length != 0)
						.ToArray())
					.ToArray();
				Console.WriteLine($"[INFO] Parsed Members: {string.Join(", ", allPeople)}");
				foreach (var player in allPeople)
				{
					var parsedName = player.Contains(']')
						? player.Split("]")[^1].Trim()
						: player;
					
					if (!PartySession.ContainsKey(parsedName.ToLower()))
						PartySession.Add(parsedName.ToLower(), parsedName);

					if (Config.ExemptPlayers.Contains(parsedName.ToLower()))
						continue;

					Config.ExemptPlayers.Add(parsedName.ToLower());
					Console.WriteLine($"\t- \"{parsedName}\" has been added to your exempt list.");
				}

				Console.WriteLine(Divider);
				return;
			}

			// API key
			if (!logImp.Contains(":") && logImp.StartsWith(ApiKeyInfo))
			{
				Config.HypixelApiKey = logImp.Split(ApiKeyInfo)[1].Trim();
				Console.WriteLine("[INFO] Received new API key. Attempting to connect...");
				var res = await ValidateApiKey(Config.HypixelApiKey);
				Console.WriteLine(res 
					? "[INFO] Connected to Hypixel's API." 
					: "[INFO] Unable to connect to Hypixel's API. Using Plancke.");
				Console.WriteLine(Divider);
				return;
			}

			// Custom MC commands.
			if (!logImp.Contains(":") && logImp.Contains(CantFindPlayer))
			{
				var commandUnparsed = logImp
					.Split(CantFindPlayerAp)[1]
					.Split('\'')[0];

				if (!commandUnparsed.StartsWith('.')) return;
				var command = commandUnparsed[1..];
#if DEBUG
				Console.WriteLine($"Command Used: {command}");
#endif
			}
		}

		/// <summary>
		/// Method that is to be executed when a file is created.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="e">Arguments.</param>
		private static async void OnChangedAsync(object source, FileSystemEventArgs e)
			// wait for image to fully load
			=> await OnChangeFileAsync(e);

		/// <summary>
		/// Method that is to be executed when a file is created.
		/// </summary>
		/// <param name="e">The arguments.</param>
		/// <param name="init">Whether the method was executed by another method (true) or by itself (false).</param>
		/// <returns>Nothing.</returns>
		private static async Task OnChangeFileAsync(FileSystemEventArgs e, bool init = true)
		{
			await Task.Delay(Config.ScreenshotDelay);

			Bitmap bitmap;
			try
			{
				bitmap = new Bitmap(ImageHelper.FromFile(e.FullPath));
			}
			catch (IOException)
			{
				if (!init)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("[ERROR] Unable to read the image.");
					Console.ResetColor();
					Console.WriteLine(Divider);
					return;
				}

				await Task.Delay(Config.ScreenshotDelay);
				await OnChangeFileAsync(e, false);
				return;
			}

			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"[ERROR] An unknown error occurred. Error Information:\n{ex}");
				Console.ResetColor();
				Console.WriteLine(Divider);
				return;
			}

			await ProcessScreenshotAsync(bitmap, e.FullPath);
			bitmap.Dispose();
		}

		/// <summary>
		/// Processes the screenshot that was provided.
		/// </summary>
		/// <param name="bitmap">The screenshot as a Bitmap.</param>
		/// <param name="path">The name of the screenshot.</param>
		/// <returns>Nothing.</returns>
		private static async Task ProcessScreenshotAsync(Bitmap bitmap, string path)
		{
			var fileInfo = new FileInfo(path);
			if (ShouldClearBeforeCheck)
				Console.Clear();

			Console.WriteLine($"[INFO] Checking Screenshot: {fileInfo.Name}");
			var processingTime = new Stopwatch();
			processingTime.Start();
			// parse time
#if USE_NEW_PARSER
			using INameParser parser = new EnhancedNameParser(bitmap, GuiScale, Config.StrictParser);
#else
			using INameParser parser = new NameParser(bitmap, GuiScale);
#endif
			var allNames = parser.ParseNames(Config.ExemptPlayers);
			// Remove all '[ ]'
			var parsedNames = new Dictionary<TeamColor, IList<string>>();
			foreach (var (color, names) in allNames)
			{
				parsedNames.Add(color, new List<string>());
				foreach (var name in names)
				{
					if (!name.Contains('[') && !name.Contains(']'))
					{
						parsedNames[color].Add(name);
						continue;
					}

					// If it contains [ or ] but not both, then it's defective. 
					if (name.Contains('[') ^ name.Contains(']'))
						continue;

					var nameNoTag = name.Split("]")[1];
					if (nameNoTag.Contains("["))
						nameNoTag = nameNoTag.Split("[")[0];

					nameNoTag = nameNoTag.Trim();
					if (nameNoTag == string.Empty)
						continue;

					parsedNames[color].Add(nameNoTag);
				}
			}

			if (parsedNames.Count == 0)
			{
				Console.WriteLine("[INFO] No parseable names found. Skipping.");
				Console.WriteLine(Divider);
				return;
			}

			// end parse
			processingTime.Stop();
			var timeTaken = processingTime.Elapsed;
			var parsedPeople = parsedNames.Sum(x => x.Value.Count);
			var entries = new[]
			{
				$"Type: {(parser.IsLobby ? "Lobby" : "Game")}",
				$"Players: {parsedPeople}"
			};
			Console.WriteLine("[" + string.Join(", ", entries) + "]");

			if (HypixelApi is not null && ApiKeyValid)
			{
				var res = await HypixelApi.ValidateApiKeyAsync();
				if (!res.Success)
				{
					HypixelApi = null;
					ApiKeyValid = false;
				}
			}
			
			if (parser.IsLobby)
			{
				if (parsedNames.ContainsKey(TeamColor.Unknown))
					await ProcessLobbyScreenshotAsync(parsedNames[TeamColor.Unknown], timeTaken);
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine(
						"[ERROR] An error occurred with the parse results. Please take another screenshot.");
					Console.ResetColor();
					Console.WriteLine(Divider);
				}
			}
			else
				await ProcessInGameScreenshotAsync(parsedNames, timeTaken);

#if !DEBUG
			if (Config.DeleteScreenshot)
				fileInfo.Delete();
#endif
		}
	}
}