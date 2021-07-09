using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Winstreak.Cli.Utility;

namespace Winstreak.Cli.WSMain
{
	public static partial class WinstreakProgram
	{
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

			// Filter out any garbage values.
			logImp = string.Join(Environment.NewLine, logImp.Split(Environment.NewLine)
				.Where(x => !x.StartsWith("You will respawn in") 
				            && !x.StartsWith("The game starts in")
				            && !(x.Contains("has joined") && x.EndsWith(")!"))
				            && !(x.StartsWith("You will respawn in") && x.EndsWith("seconds!"))));
			
			// Handle fell into void and purchases.
			if (logImp.Contains(FellIntoVoid) || logImp.Contains(YouPurchased) || logImp.Contains(FellIntoVoidFinal))
			{
				var entries = logImp.Split(Environment.NewLine).ToList();
#if DEBUG
				var identifier = new Random().Next();
				await DebugLogger.WriteLineAsync();
				await DebugLogger.LogWriteLineAsync($"[{identifier}] Received Message: {logImp}");
				await DebugLogger.LogWriteLineAsync($"[{identifier}] Parsed: {string.Join(" || ", entries)}");
#endif
				
				var indicesToRemove = new List<int>();
				for (var i = 0; i < entries.Count; i++)
				{
					var entry = entries[i];
#if DEBUG
					await DebugLogger.LogWriteLineAsync($"[{identifier}] Got Entry: {entry}");
#endif
					
					if (entry.Contains(":")) continue;

					// xxx fell into the void.
					if (entry.EndsWith(FellIntoVoid) || entry.EndsWith(FellIntoVoidFinal))
					{
						var name = logImp.Split(FellIntoVoid)[0].Trim();
#if DEBUG
						await DebugLogger.LogWriteLineAsync($"[{identifier}] Void - Name: {name}");
#endif
						
						if (name.Length > UsernameMaxLen) continue; 
						
						if (VoidDeaths.ContainsKey(name)) VoidDeaths[name]++;
						else VoidDeaths.Add(name, 1);
						indicesToRemove.Add(i);
						continue;
					}

					// You purchased xxx.
					if (entry.StartsWith(YouPurchased))
					{
						var item = entry.Split(YouPurchased)[1].Trim();
#if DEBUG
						await DebugLogger.LogWriteLineAsync($"[{identifier}] Item - Name: {item}");
#endif
						if (ItemStatistics.ContainsKey(item)) ItemStatistics[item]++;
						else ItemStatistics.Add(item, 1);
						indicesToRemove.Add(i);
					}
				}

				indicesToRemove.Reverse();
				foreach (var index in indicesToRemove)
					entries.RemoveAt(index);

				logImp = string.Join(Environment.NewLine, entries);
			}

			// Party stuff + API key
			// Handle various cases
			if (!logImp.Contains(":"))
			{
				// Joined the party
				if (logImp.Contains(JoinedParty))
				{
					var name = logImp
						.Replace("-----------------------------", string.Empty)
						.Trim()
						.Split(JoinedParty)[0]
						.Trim();
					if (name[0] == '[')
						name = name.Split(']')[1].Trim();
					OutputDisplayer.WriteLine(LogType.Info, $"{name} has joined the party.");

					if (!PartySession.ContainsKey(name.ToLower()))
						PartySession.Add(name.ToLower(), name);

					if (!Config.ExemptPlayers.Contains(name.ToLower()))
					{
						Config.ExemptPlayers.Add(name.ToLower());
						OutputDisplayer.WriteLine(LogType.Info, $"\"{name}\" has been added to your exempt list.");
					}

					Console.WriteLine(Divider);
					return;
				}

				// Removed from party.
				if (logImp.Contains(RemovedFromParty))
				{
					var name = logImp
						.Replace("-----------------------------", string.Empty)
						.Trim()
						.Split(RemovedFromParty)[0]
						.Trim();
					if (name[0] == '[')
						name = name.Split(']')[1].Trim();
					OutputDisplayer.WriteLine(LogType.Info, $"{name} has been removed from the party.");

					PartySession.Remove(name.ToLower());
					if (NamesInExempt.Any(x => string.Equals(x, name, StringComparison.CurrentCultureIgnoreCase)))
					{
						Console.WriteLine(Divider);
						return;
					}

					Config.ExemptPlayers.Remove(name.ToLower());
					OutputDisplayer.WriteLine(LogType.Info, $"\"{name}\" has been removed from your exempt list.");
					Console.WriteLine(Divider);
					return;
				}

				// You left the party.
				if (logImp.Contains(YouLeftParty) || logImp.Contains(DisbandParty))
				{
					OutputDisplayer.WriteLine(LogType.Info, "You left your current party!");
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
				if (logImp.Contains(TheyLeftParty))
				{
					var name = logImp
						.Replace("-----------------------------", string.Empty)
						.Trim()
						.Split(TheyLeftParty)[0]
						.Trim();
					if (name[0] == '[')
						name = name.Split(']')[1].Trim();
					OutputDisplayer.WriteLine(LogType.Info, $"{name} has left the party!");

					if (NamesInExempt.Any(x => string.Equals(x, name, StringComparison.CurrentCultureIgnoreCase)))
					{
						Console.WriteLine(Divider);
						return;
					}

					PartySession.Remove(name.ToLower());
					Config.ExemptPlayers.Remove(name.ToLower());
					OutputDisplayer.WriteLine(LogType.Info, $"\"{name}\" has been removed from your exempt list.");
					Console.WriteLine(Divider);
					return;
				}

				// Disband
				if (logImp.Contains(DisbandAlert))
				{
					OutputDisplayer.WriteLine(LogType.Info, "The party was disbanded!");
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
				
				// API key
				if (logImp.StartsWith(ApiKeyInfo))
				{
					Config.HypixelApiKey = logImp.Split(ApiKeyInfo)[1].Trim();
					OutputDisplayer.WriteLine(LogType.Info, "Received new API key. Attempting to connect...");
					var res = await ValidateApiKey(Config.HypixelApiKey);
					OutputDisplayer.WriteLine(LogType.Info, res
						? "Connected to Hypixel's API."
						: "Unable to connect to Hypixel's API. Using Plancke.");
					if (res && Config.FileData != default)
					{
						// Overwrite old API key.
						for (var i = 0; i < ConfigRaw.Length; i++)
						{
							if (!ConfigRaw[i].StartsWith("HYPIXEL_API_KEY="))
								continue;
							ConfigRaw[i] = $"HYPIXEL_API_KEY={Config.HypixelApiKey}";
							break;
						}

						await File.WriteAllLinesAsync(Config.FileData.FullName, ConfigRaw);
					}

					Console.WriteLine(Divider);
					return;
				}
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

#if DEBUG
				await DebugLogger.LogWriteLineAsync($"/who Received: {logImp}");
				await DebugLogger.LogWriteLineAsync($"Parsed: {string.Join(" | ", names)}");
				await DebugLogger.WriteLineAsync();
#endif
				if (names.Count == 0) return;
				OutputDisplayer.WriteLine(LogType.Info, "Received /who Command Output.");
				await ProcessLobbyScreenshotAsync(names, TimeSpan.FromMinutes(0));
				return;
			}

			// /p list used.
			// Guaranteed to have a party leader.
			if (logImp.Contains("Party Leader") && logImp.Contains("Party Members (")
			                                    && logImp[..30].Trim() == "-----------------------------")
			{
				OutputDisplayer.WriteLine(LogType.Info, "Party List Output Received.");
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
				OutputDisplayer.WriteLine(LogType.Info, $"Parsed Members: {string.Join(", ", allPeople)}");
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
	}
}