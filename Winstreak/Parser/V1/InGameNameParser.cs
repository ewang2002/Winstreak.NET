﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Winstreak.Extensions;
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

		public void AccountForTeamLetters()
		{
			int newTopLeftX = -1;
			for (int x = StartingPoint.X + 6 * GuiWidth; x < EndingPoint.X; x++)
			{
				int numParticles = NumberParticlesInVerticalLine(x);
				if (numParticles < 30)
					continue;

				newTopLeftX = x;
				break;
			}

			if (newTopLeftX == -1)
				throw new InvalidImageException("Invalid image given.");

			int oldY = StartingPoint.Y;
			StartingPoint = new Point(newTopLeftX, oldY);
			SaveCroppedImage(StartingPoint.X, StartingPoint.Y, EndingPoint.X - StartingPoint.X, EndingPoint.Y - 2 * GuiWidth - StartingPoint.Y);
		}


		public override (IList<string> lobby, IDictionary<TeamColors, IList<string>> team) GetPlayerName(IList<string> exempt = null)
		{
			exempt ??= new List<string>();

			IDictionary<TeamColors, IList<string>> teammates = new Dictionary<TeamColors, IList<string>>();
			IList<TeamColors> colorsToIgnore = new List<TeamColors>();

			TeamColors currentColor = TeamColors.Unknown;
			int y = StartingPoint.Y;
			while (y <= EndingPoint.Y)
			{
				StringBuilder name = new StringBuilder();
				int x = StartingPoint.X;

				while (true)
				{
					StringBuilder ttlBytes = new StringBuilder();
					bool errored = false;

					while (ttlBytes.Length == 0 || ttlBytes.ToString().Substring(ttlBytes.Length - 8) != "00000000")
					{
						StringBuilder columnBytes = new StringBuilder();
						for (int dy = 0; dy < 8 * base.GuiWidth; dy += base.GuiWidth)
						{
							if (y + dy >= Img.Height)
							{
								errored = true;
								break;
							}

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

						if (errored)
						{
							break;
						}

						ttlBytes.Append(columnBytes.ToString());
						x += base.GuiWidth;
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

				y += 9 * base.GuiWidth;
			}

			return (new List<string>(), teammates);
		}

		private TeamColors GetCurrentColor(Color color)
		{
			return BlueTeamColor.IsRgbEqualTo(color)
				? TeamColors.Blue
				: RedTeamColor.IsRgbEqualTo(color)
					? TeamColors.Red
					: YellowTeamColor.IsRgbEqualTo(color)
						? TeamColors.Yellow
						: GreenTeamColor.IsRgbEqualTo(color)
							? TeamColors.Green
							: TeamColors.Unknown;
		}

		/// <summary>
		/// Determines if a color is a valid team color.
		/// </summary>
		/// <param name="color">The color.</param>
		/// <returns>Whether the color is valid or not.</returns>
		public override bool IsValidColor(Color color)
		{
			return RedTeamColor.IsRgbEqualTo(color)
			       || BlueTeamColor.IsRgbEqualTo(color)
			       || YellowTeamColor.IsRgbEqualTo(color)
			       || GreenTeamColor.IsRgbEqualTo(color);
		}
	}
}