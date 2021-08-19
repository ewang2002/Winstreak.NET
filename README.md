<p align="center">
  <img src="https://github.com/ewang2002/Winstreak.NET/blob/master/ws_github.png" alt="Winstreak Introduction"/>
</p>

Winstreak is a program that has several purposes in mind. It is designed to ensure you, the player:
- Can maintain a winstreak (or snipe other people's winstreak) in Hypixel's popular Bedwars game. 
- Can avoid lobbies that may contain potential tryhard players.
- Can figure out which teams to target first based on overall team statistics. 

Winstreak is not a mod. Winstreak is a standalone application that utilizes Minecraft's screenshot feature and tab menu to parse names. Winstreak *can* read from your Minecraft logs, but this is only to enhance your experience. Feel free to go through the source code.

## Technologies
- C# Programming Language.
- .NET 5.

## Purpose
The creation of this program came as a frustration I had with Bedwars. One of the main issues I have with the game is that inexperienced Bedwars players can be placed in a lobby filled with extremely experienced players, thus making the game not so fun for inexperienced players.

My friends and I have often dealt with these experienced players. To say the least, it was not remotely fun when we're in a game with these players. 

A somewhat better solution, of course, would be putting players in lobbies with other players that have similar stats (i.e. matchmaking). But, that has not been implemented (or, it has been poorly implemented).

This program is designed, in part, to acknowledge these issue.

## Features
| Feature  | Details | Requirement(s) | 
| ------------- | ------------- | ------------- | 
| Lobby Checker  | Winstreak can check a lobby screenshot for any potential tryhards. The program will go through each player's stats and tell you if you should leave or not. | None. |
| Game Checker  | Winstreak can check an in-game screenshot and will tell you exactly which teams you should target or be careful around. It'll check each team's stats to show you where your team stands compared to opposing teams.  | None. |
| Group Checker | Generally speaking, people that are in coordinated parties are also friends. So, it follows that the best way to check for potential parties is to check each person's friends list. For both lobby & game checkers, Winstreak will be able to check each player's friends to see if there are any potential parties. | Hypixel API Key. | 
| IGN Lookup | You can directly type names into the console window to get an overview of a player's stats. And of course, you can put multiple names to compare them. | None. |
| Void Counter | Winstreak can tell you how many people have fallen into the void. Useful for making fun of those that might have fallen into the void many times. Note that the void counter may be off by a bit. | Minecraft Logs. | 
| Item Tracker | Winstreak can tell you what items you have purchased throughout your session. | Minecraft Logs. |
| /who Parser | If you don't want to take screenshots of your game for the lobby checker, you can run /who and Winstreak can parse that information and give you the lobby data. | Minecraft Logs. | 
| Party Watcher | Winstreak will watch Minecraft chat for people joining or leaving parties. From there, Winstreak can automatically add or remove people from an exempt list (people in this list won't be tracked when you run the checker commands). | Minecraft Logs. |

## Requirements
Winstreak can be used with any Minecraft version. However, do not use texture packs that change the Minecraft font, colors, or layout. In particular, the tab menu needs to be preserved (i.e. needs to look just like vanilla Minecraft).

I can only guarantee that Winstreak works with the vanilla Minecraft client. 
- I have been told that Winstreak works on Badlion client.
- Winstreak will work on Lunar, but you must disable the moon logo that shows up in the Tab list.

## Setup
You can download an executable associated with the operating system that you use from the Releases page. After you do that, fill out the `ExampleWsCliConfig.txt` file that is included in this repository and rename it to `wsconfig.txt`.

If you are on Windows and ran the executable, you may have noticed that the text does not display correctly. This is because I use ANSI color codes to display colors, which by default is disabled on Windows 10 (but can be enabled). See this [post](https://superuser.com/a/1300251) to learn how to enable it.

This program has been tested with Windows. I have not tested this program with Linux and Mac; however, it should work with both.

The executable files are significantly bigger than what I wanted them to be. However, this is because the .NET runtime is included in the executables so you do not need to download .NET. 

If you need a Hypixel API key, simply log into Hypixel and run `/api new` in the lobby.

If you so desire, you may compile the code yourself. You may either choose to install [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0) and compile the code from the command line or install [Visual Studio](https://visualstudio.microsoft.com/downloads/) and follow the directions if you want an easy way to edit the code.

## Disclaimer
The code is provided as-is. I am not responsible for any issues that may arise from your use of this program. That being said, I have used this program for over three months, with an API key, and have encountered no issues whatsoever.

Although Winstreak is *not* a mod, it may still be seen as an application that provides an advantage over other players. As far as I'm aware, the Hypixel Administrators have not provided a definitive answer as to whether something like Winstreak is allowed. Thus, as I mentioned, *use this at your own risk.* If you desire full anonymity (i.e. you don't want Hypixel to track you), do not provide your API key.

Keep in mind that, although Winstreak might mark players as bad, the players in question may either be competent or an alternative account of a professional. It is up to *your* judgement to decide whether you can take on a lobby. Winstreak only provides *assistance*; you should not only rely on Winstreak to decide whether you stay in or leave a lobby.

I have included the original image from the Minecraft source code that was used to determine the layout of each character. The image contains most (if not all) of the Minecraft characters. The file's name is `ascii.png`. The corresponding layout of each character can be found in the [Constants.cs](https://github.com/ewang2002/Winstreak.NET/blob/master/Winstreak.Core/Parsers/ImageParser/Constants.cs) file.

## Current Status
I have effectively stopped playing Bedwars. Thus, I will only provide critical fixes as needed. No new features will be added.

This project has been made open-source. I hope someone may find this helpful. 

## Credits
I came up with the original idea. However, [@icicl](https://github.com/icicl/) implemented the idea first and the name parser implementation is from his implementation, although I have changed parts of the code significantly to better handle potential edge cases.

## License
All code except for the ones listed below are licensed under the MIT license.

All code that is in `Winstreak.Core/Parsers/ImageParser/Imaging` was taken from the [Accord.NET](https://github.com/accord-net/framework) library and modified to suit my needs. These files are licensed under the [GNU Lesser General Public License](https://github.com/accord-net/framework/blob/development/LICENSE).

If any licensing issues occur, please submit an Github issue.