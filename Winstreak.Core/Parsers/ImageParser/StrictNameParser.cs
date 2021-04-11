using System;
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
	/// Along with parsing a screenshot for multiple columns, this name parser will also attempt to find a specific set
	/// of colors that can be used with the screenshot -- while this sacrifices performance, this will make parsing
	/// screenshots more accurate. 
	/// </summary>
	public class StrictNameParser : INameParser
	{
		#region Private Fields

		private readonly UnmanagedImage _img;
		private readonly int _guiWidth;

		private bool _calledBefore;

		// We set the max iterations to 10
		// Theoretically, there will be 4 (8 if in-game) columns possible of player names. 
		private int _iterations;

		private Point _startingPoint;
		private Point _endPoint;

		#endregion

		#region Constant Fields

		private static readonly Predicate<Color>[] ColorFunctions =
		{
			IsValidRankColor,
			IsValidTeamColor,
		};

		#endregion

		/// <summary>
		/// Whether the screenshot represents a lobby.
		/// </summary>
		public bool IsLobby { get; private set; } = true;

		/// <summary>
		/// Instantiates a new EnhancedNameParser object with the specified Bitmap and GUI scale.
		/// </summary>
		/// <param name="image">The bitmap.</param>
		/// <param name="guiScale">The GUI scale.</param>
		public StrictNameParser(Bitmap image, int guiScale = 2)
		{
			_img = UnmanagedImage.FromManagedImage(image);
			_guiWidth = guiScale;
			_startingPoint = new Point(4, 20 * _guiWidth);
			var maxY = 20 * _guiWidth + 9 * _guiWidth * 22;
			_endPoint = new Point(_img.Width - 4, maxY > _img.Height - 4 ? _img.Height - 4 : maxY);
		}

		/// <summary>
		/// Finds the start of a name.
		/// </summary>
		/// <param name="func">The function that can be used for this.</param>
		/// <returns>Whether there is a name that can be parsed.</returns>
		private bool FindStartOfName(out Predicate<Color> func)
		{
			func = color => false;

			// Because there can never be more than 10 columns. 
			if (_iterations++ > 10)
				return false;

			// To prevent potential out-of-bounds errors. 
			if (_startingPoint.X + 1 > _img.Width)
				return false;

			var startX = _startingPoint.X + 1;
			var endX = _calledBefore
				? startX + 150 * _guiWidth
				: _endPoint.X;

			if (!_calledBefore)
				_calledBefore = true;

			foreach (var colorFunc in ColorFunctions)
			{
				var y = _startingPoint.Y;
				var realX = -1;
				for (; y <= _endPoint.Y; y += 9 * _guiWidth)
				{
					for (var x = startX; x < endX && x < _endPoint.X; x++)
					{
						var foundValidColor = false;
						for (var dy = 0; dy < 8 * _guiWidth; dy += _guiWidth)
						{
							var p0 = _img[x, y + dy];
							var p1 = _img[x + 1, y + dy];
							var p2 = _img[x + 2, y + dy];
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

						do
						{
							var columnBytes = new StringBuilder();
							for (var dy = 0; dy < 8 * _guiWidth && tempX < _endPoint.X; dy += _guiWidth)
							{
								var pixel = _img[tempX, y + dy];
								columnBytes.Append(colorFunc(pixel) || Color.White.IsRgbEqualTo(pixel)
									? "1"
									: "0");
							}

							ttlBytes.Append(columnBytes);
							tempX += _guiWidth;

							if (tempX >= _img.Width)
								return false;
						} while (ttlBytes.ToString()[(ttlBytes.Length - 8)..] != "00000000");

						ttlBytes = new StringBuilder(ttlBytes.ToString()[..(ttlBytes.Length - 8)]);
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
					continue;

				_startingPoint = new Point(realX, y);
				func = colorFunc;
				return true;
			}

			return false;
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
			var longestX = -1;
			var tempNames = new Dictionary<TeamColor, List<(string name, bool isRed)>>();
			var hasCompletedOneIteration = false;

			// iterate over each column 
			while (FindStartOfName(out var func))
			{
				var innerTemp = new Dictionary<TeamColor, IList<(string name, bool isRed)>>();
				// Name parsing
				var numEmpty = 0;
				var y = _startingPoint.Y;
				for (; y <= _endPoint.Y; y += 9 * _guiWidth)
				{
					var isWhite = false;
					var name = new StringBuilder();
					var x = _startingPoint.X;
					var isRed = false;

					var determinedColor = new Color();
					var colorDict = new Dictionary<Color, int>();

					// go through each letter in the name until we reach the
					// end of the name
					while (true)
					{
						var ttlBytes = new StringBuilder();

						// iterate over each letter in the name 
						do
						{
							var columnBytes = new StringBuilder();

							// iterate over each pixel in a letter defined as GuiWidth
							// go down until you reach the bottom of the letter, then 
							// go back to the defined y-value and add to the x-coord
							// to go through the next "column"
							for (var dy = 0; dy < 8 * _guiWidth; dy += _guiWidth)
							{
								var color = _img[x, y + dy];
								var isWhiteTemp = Color.White.IsRgbEqualTo(color);

								// "determinedColor" is the "lock" color.
								// basically, once we determine what the color of the
								// first letter is, we can lock onto that color.
								// in other words, once "determinedColor" is defined (i.e.
								// not default), then we ONLY check and see if the pixel
								// is equal to "isDeterminedColor"
								var isPixelCorrect = determinedColor == default
									? func(color) || isWhiteTemp
									: determinedColor.IsRgbEqualTo(color);

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
							x += _guiWidth;
						}
						// "00000000" represents an empty vertical line that separates
						// a letter from another letter
						while (ttlBytes.ToString()[(ttlBytes.Length - 8)..] != "00000000");

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
								x += 5 * _guiWidth;
							else if (BinaryToCharactersMap[ttlBytes.ToString()] == "]")
							{
								name.Append(' ');
								x += 4 * _guiWidth;
							}

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
					} // end inner while loop 

					// this is our final name.
					var finalName = name.ToString();
					// if the name string is empty, then it's invalid
					if (finalName.Trim() == string.Empty)
					{
						numEmpty++;
						if (numEmpty > 5)
							break;

						continue;
					}

					// OR the color of the name is white
					// but no space at beginning of name, then
					// we have an invalid name
					if (isWhite && name.ToString()[0] != ' ')
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
					if (!innerTemp.ContainsKey(currentColor))
						innerTemp.Add(currentColor, new List<(string, bool)>());

					if (isGameScreenshot)
					{
						if (finalName.Trim().IndexOf(' ') != -1)
							innerTemp[currentColor].Add((finalName.Trim().Split(" ")[1], isRed));
					}
					else
						innerTemp[currentColor].Add((finalName.Trim(), isRed));

					// last thing: set to X if this is the farthest so far
					if (longestX < x)
						longestX = x;
				} // end of for loop 

				// Check to make sure all the names are valid
				var isNotOk = innerTemp
					.SelectMany(x => x.Value)
					.All(x => x.name.Length <= 2 && int.TryParse(x.name, out _));

				if (!isNotOk)
					foreach (var (k, v) in innerTemp)
					{
						if (tempNames.ContainsKey(k))
							tempNames[k].AddRange(v);
						else
							tempNames.Add(k, new List<(string name, bool isRed)>(v));
					}

				// Result
				if (!hasCompletedOneIteration)
				{
					hasCompletedOneIteration = true;
					_endPoint = new Point(_img.Width - _startingPoint.X, y + 9 * _guiWidth);
				}

				_startingPoint = new Point(longestX, 20 * _guiWidth);
			} // end of outer while loop 

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
		public void Dispose() => _img?.Dispose();

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