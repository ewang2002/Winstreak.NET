<p align="center">
  <img src="https://github.com/ewang2002/Winstreak.NET/blob/master/ws_github.png" alt="Winstreak Introduction"/>
</p>

Winstreak is a program that has several purposes in mind. It is designed to ensure you, the player:
- Can maintain a winstreak in Hypixel's popular Bedwars game. 
- Can avoid lobbies that may contain potential tryhard players.
- Can figure out which teams to target first based on overall team statistics. 

Winstreak is not a mod. Winstreak is a standalone application that utilizes Minecraft's screenshot feature and tab menu to parse names. Winstreak does not read from your Minecraft log files, either. 

## Technologies
- C# Programming Language.
- .NET 5 

## Purpose
The creation of this program came as a frustration I had with Bedwars. One of the main issues I have with the game is that inexperienced Bedwars players can be placed in a lobby filled with extremely experienced players, thus making the game not so fun for inexperienced players.

My friends and I have often dealt with these experienced players. To say the least, it was not remotely fun when we're in a game with these players. 

A somewhat better solution, of course, would be putting players in lobbies with other players that have similar stats. But, of course, we all know Hypixel enjoys listening to their players.

This program is designed, in part, to acknowledge this issue. 

## Features
Winstreak boasts several key features.
- **Lobby Checker**: Winstreak can check a lobby screenshot for any potential tryhards. The program will go through each player's stats and tell you if you should leave or not.
- **Game Checker**: Winstreak can check an in-game screenshot and will tell you exactly which team you should target or be careful around. It'll check each team's stats to show you where your team stands compared to opposing teams.
- **Group Checker**: For both lobby & game checkers, Winstreak will be able to check each player's friends to see if there are any potential parties.*
- **IGN Lookup**: You can directly type names into the console window to get an overview of a player's stats. And of course, you can put multiple names to compare them.

\* - Requires Hypixel API Key.

## Requirements
Winstreak can be used with any Minecraft version. However, do not use texture packs that change the Minecraft font, colors, or layout. In particular, the tab menu needs to be preserved (i.e. needs to look just like vanilla Minecraft).

I can only guarantee that Winstreak works with the vanilla Minecraft client. Winstreak, at the moment, does NOT support with Lunar client. In particular, Winstreak is known for not being able to detect names when the Lunar logo (the moon) appears next to a person's name. 

## Setup
You can download an executable associated with the operating system that you use from the Releases page. After you do that, fill out the `ExampleWsCliConfig.txt` file that is included in this repository and rename it to `wsconfig.txt`.

If you are on Windows and ran the executable, you may have noticed that the text does not display correctly. This is because I use ANSI color codes to display colors, which by default is disabled on Windows 10 (but can be enabled). See this [post](https://superuser.com/a/1300251) to learn how to enable it.

This program has been tested with Windows. I have not tested this program with Linux and Mac; however, it should work with both.

The executable files are significantly bigger than what I wanted them to be. However, this is because the .NET runtime is included in the executables so you do not need to download .NET. 

## Credits
I came up with the original idea. However, [@icicl](https://github.com/icicl/) implemented the idea first and the name parser implementation is from his implementation, although I have changed parts of the code significantly to better handle potential edge cases.

## License
All code except for the ones listed below are licensed under the MIT license.

All code that is in `Winstreak.Core/Parsers/ImageParser/Imaging` was taken from the excellent [Accord.NET](https://github.com/accord-net/framework) library and modified to suit my needs. These files are licensed under the GNU Lesser General Public License.