using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Winstreak.Extensions;
using Winstreak.Imaging;
using Winstreak.Parser.ImgExcept;
using static Winstreak.Parser.Constants;

namespace Winstreak.Parser
{
	/// <summary>
	/// Parses a screenshot containing the member tab list.
	/// </summary>
	public sealed class NameParser : IDisposable
	{
		/// <summary>
		/// The image.
		/// </summary>
		public UnmanagedImage Img { get; }

		/// <summary>
		/// Whether the screenshot represents a lobby.
		/// </summary>
		public bool IsLobby { get; private set; } = true;

		/// <summary>
		/// Minecraft's GUI width.
		/// </summary>
		public int GuiWidth { get; private set; }

		/// <summary>
		/// The starting point; i.e., where the first name is located.
		/// </summary>
		public Point StartingPoint { get; private set; }

		/// <summary>
		/// The ending point.
		/// </summary>
		public Point EndingPoint { get; private set; }

		/// <summary>
		/// The Bedwars mode. Valid numbers are 12 (solos/doubles) or 4/4 (3v3v3v3s, 4v4v4v4s, 4v4s) or their direct dream equivalent(s). 
		/// </summary>
		public int Mode { get; private set; } = 34;

		/// <summary>
		/// Instantiates a new NameParser object with the specified Bitmap.
		/// </summary>
		/// <param name="image">The bitmap.</param>
		public NameParser(Bitmap image) => Img = UnmanagedImage.FromManagedImage(image);

		/// <summary>
		/// Sets the Gui scale.
		/// </summary>
		/// <param name="scale">The scale.</param>
		public void SetGuiScale(int scale) => GuiWidth = scale;

		/// <summary>
		/// Sets the game mode for this parser. Selecting a mode will tell the parser to parse any in-game tab screenshots based on the selected mode (if you select Solos/Doubles, all 8 colors will be accounted for; if you select 3s/4s, only R/G/Y/B will be considered). 
		/// </summary>
		/// <param name="mode">The game mode for this parser. The game mode must be one of the following: 12 (Solos/Doubles), 34 (3v3v3v3 or 4v4v4v4 or 4v4s).</param>
		public void SetGameMode(int mode)
		{
			if (mode != 12 && mode != 34)
				throw new ArgumentOutOfRangeException(
					$"Given mode \"{mode}\" is invalid. You must select one of the following modes: 12 (Solos/Doubles), 34 (3v3v3v3s/4v4v4v4s/4v4s).");
			Mode = mode;
		}

		/// <summary>
		/// Finds the starting and ending point of the image. 
		/// </summary>
		public void InitPoints()
		{
			StartingPoint = new Point(Img.Width / 4, 20 * GuiWidth);
			EndingPoint = new Point(Img.Width - Img.Width / 4, Img.Height / 2);
		}

		/// <summary>
		/// Finds the start of the name.
		/// </summary>
		/// <returns></returns>
		public void FindStartOfName()
		{
			var y = StartingPoint.Y;
			var realX = -1;
			var startX = StartingPoint.X;
			var endX = Img.Width - startX;

			for (; y <= EndingPoint.Y; y += 9 * GuiWidth)
			{
				for (var x = startX; x < endX; x++)
				{
					var foundValidColor = false;
					for (var dy = 0; dy < 8 * GuiWidth; dy += GuiWidth)
					{
						var p0 = Img[x, y + dy];
						var p1 = Img[x + 1, y + dy];
						var p2 = Img[x + 2, y + dy];
						if (!IsValidRankColor(p0)
						    && !IsValidTeamColor(p0)
						    && (!Color.White.IsRgbEqualTo(p0)
						        || !IsValidRankColor(p1)
						        && !IsValidTeamColor(p1)
						        && !Color.White.IsRgbEqualTo(p1)
						        && !IsValidRankColor(p2)
						        && !IsValidTeamColor(p2)
						        && !Color.White.IsRgbEqualTo(p2)))
							continue;

						foundValidColor = true;
						break;
					}

					if (!foundValidColor)
						continue;

					var ttlBytes = new StringBuilder();
					var tempX = x;

					// gets one character 
					while (ttlBytes.Length == 0 || ttlBytes.ToString().Substring(ttlBytes.Length - 8) != "00000000")
					{
						var columnBytes = new StringBuilder();
						for (var dy = 0; dy < 8 * GuiWidth && tempX < EndingPoint.X; dy += GuiWidth)
						{
							var pixel = Img[tempX, y + dy];
							columnBytes.Append(IsValidRankColor(pixel)
							                   || IsValidTeamColor(pixel)
							                   || Color.White.IsRgbEqualTo(pixel)
								? "1"
								: "0");
						}

						ttlBytes.Append(columnBytes.ToString());
						tempX += GuiWidth;
					}

					ttlBytes = new StringBuilder(ttlBytes.ToString().Substring(0, ttlBytes.Length - 8));

					if (!BinaryToCharactersMap.ContainsKey(ttlBytes.ToString()))
						continue;

					realX = x;
					break;
				}


				// end for
				if (realX != -1)
					break;
			}

			if (realX == -1)
				throw new InvalidImageException("Couldn't find any Minecraft characters.");

			StartingPoint = new Point(realX, y);
		}

		/// <summary>
		/// Parses the names from a screenshot. If the screenshot is a lobby screenshot, then there will only be one key: "Unknown."
		/// </summary>
		/// <param name="exempt">The list of players to not check.</param>
		/// <returns>The parsed names.</returns>
		public IDictionary<TeamColor, IList<string>> ParseNames(IList<string> exempt = null)
		{
			var isGameScreenshot = false;

			exempt ??= new List<string>();
			var currentColor = TeamColor.Unknown;

			var names = new Dictionary<TeamColor, IList<string>>();
			var tempNames = new Dictionary<TeamColor, IList<(string name, bool isRed)>>();
			for (var y = StartingPoint.Y; y <= EndingPoint.Y; y += 9 * GuiWidth)
			{
				var isWhite = false;
				var name = new StringBuilder();
				var x = StartingPoint.X;
				var isRed = false;
				while (true)
				{
					var ttlBytes = new StringBuilder();

					while (ttlBytes.Length == 0
					       || ttlBytes.ToString().Substring(ttlBytes.Length - 8) != "00000000")
					{
						var columnBytes = new StringBuilder();
						for (var dy = 0; dy < 8 * GuiWidth; dy += GuiWidth)
						{
							var color = Img[x, y + dy];
							var isRankColor = IsValidRankColor(color);
							var isTeamColor = IsValidTeamColor(color);
							var isWhiteTemp = Color.White.IsRgbEqualTo(color);

							if (isRankColor || isWhiteTemp || isTeamColor)
							{
								if (isWhiteTemp)
									isWhite = true;
								else if (RedTeamColor.IsRgbEqualTo(color))
									isRed = true;

								columnBytes.Append("1");
							}
							else
								columnBytes.Append("0");
						}

						ttlBytes.Append(columnBytes.ToString());
						x += GuiWidth;
					}

					ttlBytes = new StringBuilder(ttlBytes.ToString()
						.Substring(0, ttlBytes.Length - 8));

					if (BinaryToCharactersMap.ContainsKey(ttlBytes.ToString()))
					{
						name.Append(BinaryToCharactersMap[ttlBytes.ToString()]);
						if (BinaryToCharactersMap[ttlBytes.ToString()][0] == ' ')
							x += 5 * GuiWidth;
					}
					else
						break;
				}

				var finalName = name.ToString();

				// no name, no go
				if (finalName.Trim() == string.Empty
				    || isWhite && name.ToString()[0] != ' ')
					continue;

				// team screenshots are in the format
				// " L name" where L = team char
				if (finalName[0] == ' ')
				{
					var teamAndName = finalName.Trim()
						.Split(" ");
					currentColor = GetCurrentColor(teamAndName[0][0]);
					isGameScreenshot = true;
				}

				if (!isGameScreenshot && exempt.Contains(finalName.Trim().ToLower()))
					continue;

				if (!tempNames.ContainsKey(currentColor))
					tempNames.Add(currentColor, new List<(string, bool)>());

				if (isGameScreenshot)
				{
					if (finalName.Trim().IndexOf(' ') != -1)
						tempNames[currentColor].Add((finalName.Trim().Split(" ")[1], isRed));
				}
				else
					tempNames[currentColor].Add((finalName.Trim(), isRed));
			}

			IsLobby = !isGameScreenshot;
			// if in lobby screenshot
			// remove any red names
			foreach (var (key, val) in tempNames)
				names[key] = IsLobby
					? val.Where(x => !x.isRed).Select(x => x.name).ToList()
					: val.Select(x => x.name).ToList();

			return names;
		}

		/// <summary>
		/// Disposes the image.
		/// </summary>
		public void Dispose() => Img?.Dispose();

		/// <summary>
		/// Whether the color specified is a valid color.
		/// </summary>
		/// <param name="color">The color to check.</param>
		/// <returns>Whether the color is valid.</returns>
		private bool IsValidRankColor(Color color)
			=> MvpPlusPlus.IsRgbEqualTo(color)
			   || MvpPlus.IsRgbEqualTo(color)
			   || Mvp.IsRgbEqualTo(color)
			   || VipPlus.IsRgbEqualTo(color)
			   || Vip.IsRgbEqualTo(color)
			   || None.IsRgbEqualTo(color);

		/// <summary>
		/// Determines whether a color is a valid team color.
		/// </summary>
		/// <param name="color">The team color.</param>
		/// <returns>Whether the color is valid or not.</returns>
		private bool IsValidTeamColor(Color color)
		{
			var generalColors = RedTeamColor.IsRgbEqualTo(color)
			                    || GreenTeamColor.IsRgbEqualTo(color)
			                    || YellowTeamColor.IsRgbEqualTo(color)
			                    || BlueTeamColor.IsRgbEqualTo(color);

			var accountForOnesTwos = GreyTeamColor.IsRgbEqualTo(color)
			                         || AquaTeamColor.IsRgbEqualTo(color)
			                         || PinkTeamColor.IsRgbEqualTo(color)
			                         || WhiteTeamColor.IsRgbEqualTo(color);

			return Mode == 12
				? generalColors || accountForOnesTwos
				: generalColors;
		}

		/// <summary>
		/// Gets the current team color.
		/// </summary>
		/// <param name="letter">The letter.</param>
		/// <returns>The team color as an enum flag.</returns>
		private static TeamColor GetCurrentColor(char letter)
			=> letter switch
			{
				'R' => TeamColor.Red,
				'G' => TeamColor.Green,
				'Y' => TeamColor.Yellow,
				'B' => TeamColor.Blue,
				// doubles, solos only
				'A' => TeamColor.Aqua,
				'S' => TeamColor.Grey,
				'W' => TeamColor.White,
				'P' => TeamColor.Pink,
				_ => TeamColor.Unknown
			};
	}
}