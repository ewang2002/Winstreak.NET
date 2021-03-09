using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Winstreak.Core.Extensions;
using Winstreak.Core.Parsers.ImageParser.Imaging;
using static Winstreak.Core.Parsers.ImageParser.Constants;

namespace Winstreak.Core.Parsers.ImageParser
{
	/// <summary>
	/// Parses a screenshot containing the member tab list.
	/// </summary>
	public sealed class NameParser : INameParser
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
		public Point EndingPoint { get; }

		/// <summary>
		/// Instantiates a new NameParser object with the specified Bitmap.
		/// </summary>
		/// <param name="image">The bitmap.</param>
		/// <param name="guiScale">The GUI scale.</param>
		public NameParser(Bitmap image, int guiScale = 2)
		{
			Img = UnmanagedImage.FromManagedImage(image);
			GuiWidth = guiScale;
			StartingPoint = new Point(Img.Width / 4, 20 * GuiWidth);
			EndingPoint = new Point(Img.Width - Img.Width / 4, Img.Height / 2);
		}

		/// <summary>
		/// Finds the start of the name.
		/// </summary>
		/// <returns>True if we can find the start of a new character. False otherwise.</returns>
		private bool FindStartOfName()
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

					// we only want to get one character
					// see "ParseNames" for explanation of
					// how this works 
					while (ttlBytes.Length == 0 || ttlBytes.ToString()[(ttlBytes.Length - 8)..] != "00000000")
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

						ttlBytes.Append(columnBytes);
						tempX += GuiWidth;
					}

					ttlBytes = new StringBuilder(ttlBytes.ToString().Substring(0, ttlBytes.Length - 8));

					if (!BinaryToCharactersMap.ContainsKey(ttlBytes.ToString()))
						continue;

					realX = x;
					break;
				}


				// end for loop
				if (realX != -1)
					break;
			}

			if (realX == -1)
				return false; 

			StartingPoint = new Point(realX, y);
			return true; 
		}

		/// <summary>
		/// Parses the names from a screenshot. If the screenshot is a lobby screenshot, then there will only be one key: "Unknown."
		/// </summary>
		/// <param name="exempt">The list of players to not check.</param>
		/// <returns>The parsed names.</returns>
		public IDictionary<TeamColor, IList<string>> ParseNames(IList<string> exempt = null)
		{
			if (!FindStartOfName())
				return new Dictionary<TeamColor, IList<string>>();
			
			var isGameScreenshot = false;
			exempt ??= new List<string>();
			var currentColor = TeamColor.Unknown;
			var names = new Dictionary<TeamColor, IList<string>>();
			var tempNames = new Dictionary<TeamColor, IList<(string name, bool isRed)>>();

			// iterate over each name entry in the tab list
			for (var y = StartingPoint.Y; y <= EndingPoint.Y; y += 9 * GuiWidth)
			{
				var isWhite = false;
				var name = new StringBuilder();
				var x = StartingPoint.X;
				var isRed = false;

				var determinedColor = new Color();
				var colorDict = new Dictionary<Color, int>();

				// go through each letter in the name until we reach the
				// end of the name
				while (true)
				{
					var ttlBytes = new StringBuilder();

					// iterate over each letter in the name 
					while (ttlBytes.Length == 0
						   // "00000000" represents an empty vertical line that separates
						   // a letter from another letter
						   || ttlBytes.ToString()[(ttlBytes.Length - 8)..] != "00000000")
					{
						var columnBytes = new StringBuilder();

						// iterate over each pixel in a letter defined as GuiWidth
						// go down until you reach the bottom of the letter, then 
						// go back to the defined y-value and add to the x-coord
						// to go through the next "column"
						for (var dy = 0; dy < 8 * GuiWidth; dy += GuiWidth)
						{
							var color = Img[x, y + dy];

							var isRankColor = IsValidRankColor(color);
							var isTeamColor = IsValidTeamColor(color);
							var isWhiteTemp = Color.White.IsRgbEqualTo(color);
							var isDeterminedColor = determinedColor != default
													&& determinedColor.IsRgbEqualTo(color)
													|| determinedColor == color;

							// "determinedColor" is the "lock" color.
							// basically, once we determine what the color of the
							// first letter is, we can lock onto that color.
							// in other words, once "determinedColor" is defined (i.e.
							// not default), then we ONLY check and see if the pixel
							// is equal to "isDeterminedColor"
							var isPixelCorrect = determinedColor == default
								? isRankColor || isWhiteTemp || isTeamColor
								: isDeterminedColor;

							// if we have a valid pixel
							if (isPixelCorrect)
							{
								if (isWhiteTemp)
									isWhite = true;
								else if (RedTeamColor.IsRgbEqualTo(color))
									isRed = true;

								// if we didn't determine what the "lock" color will
								// be, then let's add to the dictionary of encountered
								// color values
								if (determinedColor == default)
								{
									if (colorDict.ContainsKey(color))
										colorDict[color]++;
									else
										colorDict.Add(color, 1);
								}

								// append a "1," indicating that the valid color was
								// found at this pixel location
								columnBytes.Append('1');
							}
							else
								// append a "0," indicating that the valid color was 
								// not found at this pixel location.
								columnBytes.Append('0');
						}

						ttlBytes.Append(columnBytes);

						// next "column"
						x += GuiWidth;
					}

					// remove the 8 trailing 0s. we don't need those 0s. 
					ttlBytes = new StringBuilder(ttlBytes.ToString()
						.Substring(0, ttlBytes.Length - 8));

					// check and see if we found a valid letter or not.
					if (BinaryToCharactersMap.ContainsKey(ttlBytes.ToString()))
					{
						name.Append(BinaryToCharactersMap[ttlBytes.ToString()]);

						// An empty space (predefined by me) at the beginning of the
						// name means that we're in a game.
						if (BinaryToCharactersMap[ttlBytes.ToString()][0] == ' ')
							x += 5 * GuiWidth;

						// if the determined color wasn't defined, define it. 
						if (determinedColor != default)
							continue;
						colorDict = colorDict.OrderByDescending(pair => pair.Value)
							.ToDictionary(kv => kv.Key, kv => kv.Value);

						var (mostOcc, _) = colorDict.First();
						determinedColor = mostOcc;
					}
					else
						// invalid character = we reached end of name.
						break;
				}

				// this is our final name.
				var finalName = name.ToString();

				// if the name string is empty 
				// OR the color of the name is white
				// but no space at beginning of name, then
				// we have an invalid name
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

				// if we're not in-game and the name is in the list of exempt players
				// we don't need to add that name
				if (!isGameScreenshot && exempt.Contains(finalName.Trim().ToLower()))
					continue;

				// if the team color isn't in our dictionary,
				// add the team color. 
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

			// if this is a lobby screenshot
			// remove any red names as those names
			// are generally not valid.
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
		private static bool IsValidRankColor(Color color)
			=> MvpPlusPlus.IsRgbEqualTo(color)
			   || MvpPlus.IsRgbEqualTo(color)
			   || Mvp.IsRgbEqualTo(color)
			   || VipPlus.IsRgbEqualTo(color)
			   || Vip.IsRgbEqualTo(color)
			   || None.IsRgbEqualTo(color)
			   // 1.14+
			   || MvpPlusPlus == color
			   || MvpPlus == color
			   || Mvp == color
			   || VipPlus == color
			   || Vip == color
			   || None == color;

		/// <summary>
		/// Determines whether a color is a valid team color.
		/// </summary>
		/// <param name="color">The team color.</param>
		/// <returns>Whether the color is valid or not.</returns>
		private static bool IsValidTeamColor(Color color)
		{
			var generalColors = RedTeamColor.IsRgbEqualTo(color)
								|| GreenTeamColor.IsRgbEqualTo(color)
								|| YellowTeamColor.IsRgbEqualTo(color)
								|| BlueTeamColor.IsRgbEqualTo(color)
								// 1.14+ colors
								// 1.14+ screenshots use RGBA, not RGB. 
								|| RedTeamColor == color
								|| GreenTeamColor == color
								|| YellowTeamColor == color
								|| BlueTeamColor == color;

			var accountForOnesTwos = GreyTeamColor.IsRgbEqualTo(color)
									 || AquaTeamColor.IsRgbEqualTo(color)
									 || PinkTeamColor.IsRgbEqualTo(color)
									 || WhiteTeamColor.IsRgbEqualTo(color)
									 // 1.14+
									 || GreyTeamColor == color
									 || AquaTeamColor == color
									 || PinkTeamColor == color
									 || WhiteTeamColor == color;

			return generalColors || accountForOnesTwos;
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