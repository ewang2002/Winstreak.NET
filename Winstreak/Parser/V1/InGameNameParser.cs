using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Winstreak.Parser.ImgExcept;
using static Winstreak.Parser.Constants;

namespace Winstreak.Parser.V1
{
	public class InGameNameParser : AbstractNameParser
	{
		public InGameNameParser(Bitmap image) : base(image)
		{
		}

		public InGameNameParser(string file) : base(file)
		{
		}

		public override void CropImageIfFullScreen()
		{
			if (base.CalledCropIfFullScreen)
			{
				return;
			}

			base.CalledCropIfFullScreen = true;

			// top to bottom, left to right
			// find the top left coordinates of the
			// player list box
			int topLeftX = -1;
			int topLeftY = -1;

			for (int y = 0; y < base.Img.Height; y++)
			{
				bool canBreak = false;
				for (int x = 0; x < base.Img.Width; x++)
				{
					if (base.Img.GetPixel(x, y) != BossBarColor)
					{
						continue;
					}

					topLeftX = x;
					topLeftY = y;
					canBreak = true;
					break;
				}

				if (canBreak)
				{
					break;
				}
			}

			// right to left, bottom to top
			int bottomRightX = -1;
			int bottomRightY = -1;

			for (int x = base.Img.Width - ListedNumsOffset; x >= 0; x--)
			{
				bool canBreak = false;
				for (int y = base.Img.Height - 1; y >= 0; y--)
				{
					if (base.Img.GetPixel(x, y) == StoreHypixelNetDarkColor)
					{
						bottomRightX = x;
						bottomRightY = y;
						canBreak = true;
						break;
					}
				}

				if (canBreak)
				{
					break;
				}
			}

			if (topLeftX == -1 || topLeftY == -1)
			{
				throw new InvalidImageException(
					"Invalid image given. Either a player list wasn't detected or the \"background\" of the player list isn't just the sky. Make sure the image contains the player list and that the \"background\" of the player list is just the sky (no clouds).");
			}

			base.CropImage(topLeftX, topLeftY, bottomRightX - topLeftX, bottomRightY - topLeftY);
		}

		public override void FixImage()
		{
			if (base.CalledMakeBlkWtFunc)
			{
				return;
			}

			base.CalledFixImgFunc = true;
			int minStartingXVal = base.Img.Width;
			int minStartingYVal = base.Img.Height;
			for (int y = 0; y < base.Img.Height; y++)
			{
				for (int x = 0; x < base.Img.Width; x++)
				{
					if (!IsValidColor(base.Img.GetPixel(x, y)))
					{
						continue;
					}

					if (x < minStartingXVal)
					{
						minStartingXVal = x;
					}

					if (y < minStartingYVal)
					{
						minStartingYVal = y;
					}
				}
			}

			int startingXVal = minStartingXVal;
			int startingYVal = minStartingYVal;

			if (startingXVal == base.Img.Width || startingYVal == base.Img.Height)
			{
				throw new InvalidImageException(
					"Couldn't crop the image. Make sure the image was processed beforehand.");
			}

			base.CropImage(startingXVal, startingYVal, base.Img.Width - startingXVal, base.Img.Height - startingYVal);


			// let's remove any blanks
			int secondY = 0;
			for (; secondY < base.Img.Height; secondY++)
			{
				if (this.IsValidColor(base.Img.GetPixel(0, secondY)) &&
				    this.IsValidColor(base.Img.GetPixel(base.Width, secondY)))
				{
					break;
				}
			}

			// now let's try again
			// but this time we're going to look
			// for the separator between the B/R/G/Y and the names of teammates
			bool foundYSep = false;
			int secondX = 0;
			for (; secondX < base.Img.Width; secondX++)
			{
				int numberParticles = base.NumberParticlesInVerticalLine(secondX);
				if (numberParticles == 0 && !foundYSep)
				{
					foundYSep = true;
				}

				if (foundYSep)
				{
					if (numberParticles == 0)
					{
						continue;
					}

					break;
				}
			}
			// now we need to determine where to start

			// make another copy
			base.CropImage(secondX, secondY, base.Img.Width - secondX, base.Img.Height - secondY);
		}

		public IDictionary<TeamColors, IList<string>> GetPlayerName(IList<string> exempt = null)
		{
			if (!base.CalledMakeBlkWtFunc && !base.CalledFixImgFunc)
			{
				return new Dictionary<TeamColors, IList<string>>();
			}

			exempt ??= new List<string>();

			IDictionary<TeamColors, IList<string>> teammates = new Dictionary<TeamColors, IList<string>>();
			IList<TeamColors> colorsToIgnore = new List<TeamColors>();

			int y = 0;

			TeamColors currentColor = TeamColors.Unknown;
			while (y <= base.Img.Height)
			{
				StringBuilder name = new StringBuilder();
				int x = 0;

				while (true)
				{
					StringBuilder ttlBytes = new StringBuilder();
					bool errored = false;

					while (ttlBytes.Length == 0 || ttlBytes.ToString().Substring(ttlBytes.Length - 8) == "00000000")
					{
						try
						{
							StringBuilder columnBytes = new StringBuilder();
							for (int dy = 0; dy < 8 * base.Width; dy += base.Width)
							{
								Color color = base.Img.GetPixel(x, y + dy);
								if (IsValidColor(color))
								{
									currentColor = GetCurrentColor(color);
									columnBytes.Append("1");
								}
								else
								{
									columnBytes.Append("0");
								}
							}

							ttlBytes.Append(columnBytes.ToString());
							x += base.Width;
						}
						catch (Exception)
						{
							errored = true;
							break;
						}
					}

					if (!errored)
					{
						ttlBytes = new StringBuilder(ttlBytes.ToString().Substring(0, ttlBytes.Length - 8));
					}

					if (BinaryToCharactersMap.ContainsKey(ttlBytes.ToString()))
					{
						name.Append(BinaryToCharactersMap[ttlBytes.ToString()]);
					}
					else
					{
						break;
					}
				}

				if (exempt.Contains(name.ToString()))
				{
					colorsToIgnore.Add(currentColor);
				}

				if (!colorsToIgnore.Contains(currentColor) && !name.ToString().Trim().Equals(string.Empty))
				{
					if (currentColor == TeamColors.Unknown)
					{
						continue;
					}

					if (!teammates.ContainsKey(currentColor))
					{
						teammates.Add(currentColor, new List<string>());
					}

					teammates[currentColor].Add(name.ToString());
				}

				y += 9 * base.Width;
			}

			return teammates; 
		}

		private TeamColors GetCurrentColor(Color color)
		{
			if (color == BlueTeamColor)
			{
				return TeamColors.Blue;
			}

			if (color == RedTeamColor)
			{
				return TeamColors.Red;
			}

			if (color == YellowTeamColor)
			{
				return TeamColors.Yellow;
			}

			if (color == GreenTeamColor)
			{
				return TeamColors.Green;
			}

			return TeamColors.Unknown;
		}

		/// <summary>
		/// Determines if a color is a valid team color.
		/// </summary>
		/// <param name="color">The color.</param>
		/// <returns>Whether the color is valid or not.</returns>
		public override bool IsValidColor(Color color)
		{
			return color == RedTeamColor
			       || color == BlueTeamColor
			       || color == YellowTeamColor
			       || color == GreenTeamColor;
		}
	}
}