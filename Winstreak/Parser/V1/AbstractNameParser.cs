using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Winstreak.Extensions;
using Winstreak.Imaging;
using Winstreak.Parser.ImgExcept;
using static Winstreak.Parser.Constants;

namespace Winstreak.Parser.V1
{
	public abstract class AbstractNameParser : IDisposable
	{
		// private general variables
		protected UnmanagedImage Img { get; private set; }
		public int GuiWidth { get; private set; }
		protected Point StartingPoint { get; set; }
		protected Point EndingPoint { get; set; }

		/// <summary>
		/// A constructor that accepts a Bitmap.
		/// </summary>
		/// <param name="image">The bitmap.</param>
		protected AbstractNameParser(Bitmap image)
		{
			Img = UnmanagedImage.FromManagedImage(image);
		}

		/// <summary>
		/// A constructor that accepts a file URL. 
		/// </summary>
		/// <param name="file">The file URL.</param>
		protected AbstractNameParser(string file)
		{
			Img = UnmanagedImage.FromManagedImage(ImageHelper.FromFile(file));
		}

		/// <summary>
		/// Establishes the Gui scale that will be used to calculate pixels.
		/// </summary>
		/// <param name="guiScale">The Gui scale.</param>
		public void SetGuiScale(int guiScale)
		{
			this.GuiWidth = guiScale;
		}

		/// <summary>
		/// Finds the starting and ending point of the image. 
		/// </summary>
		public void FindStartingPoint()
		{
			// get top left point
			int topLeftX = -1;
			int topLeftY = 20 * GuiWidth;

			for (int x = Img.Width / 4; x < Img.Width; x++)
			{
				if (YouArePlayingOnColor.IsRgbEqualTo(Img.GetPixel(x, 16 * GuiWidth)))
				{
					topLeftX = x;
					break;
				}
			}

			// right to left, bottom to top
			int bottomRightX = -1;
			int bottomRightY = -1;

			for (int x = Img.Width - ListedNumsOffset; x >= 0; x--)
			{
				bool canBreak = false;
				for (int y = Img.Height - 1; y >= 0; y--)
				{
					if (!StoreHypixelNetDarkColor.IsRgbEqualTo(Img.GetPixel(x, y)))
						continue;
					bottomRightX = x;
					bottomRightY = y;
					canBreak = true;
					break;
				}

				if (canBreak)
					break;
			}

			if (topLeftX == -1 || topLeftX == Img.Width - 1 || bottomRightY == -1 || bottomRightX == -1)
				throw new InvalidImageException(
					"Invalid image given. Either a player list wasn't detected or the \"background\" of the player list isn't just the sky. Make sure the image contains the player list and that the \"background\" of the player list is just the sky (no clouds).");

			StartingPoint = new Point(topLeftX, topLeftY);
			// subtract two because bottomRightY is right below the
			// right "roof" of the "T" 
			EndingPoint = new Point(bottomRightX, bottomRightY - 2 * GuiWidth);
		}

		/// <summary>
		/// Finds the x-value of the first name.
		///
		/// Precondition: There must be at least 4 names. 
		/// </summary>
		public void FindStartOfName()
		{
			int y = StartingPoint.Y;
			int realX = -1;

			int startX = StartingPoint.X;
			int endX = Img.Width - startX;

			bool errored = false;

			while (y <= EndingPoint.Y)
			{

				for (int x = startX; x < endX; x++)
				{
					bool foundValidColor = false;
					for (int _dy = 0; _dy < 8 * GuiWidth; _dy += GuiWidth)
					{
						if (IsValidColor(Img[x, y + _dy]) || Color.White.IsRgbEqualTo(Img[x, y + _dy]) &&
							(IsValidColor(Img[x + 1, y + _dy]) || Color.White.IsRgbEqualTo(Img[x + 1, y + _dy]) ||
							 IsValidColor(Img[x + 2, y + _dy]) || Color.White.IsRgbEqualTo(Img[x + 2, y + _dy])))
						{
							foundValidColor = true;
							break;
						}
					}

					if (foundValidColor)
					{
						StringBuilder ttlBytes = new StringBuilder();

						int tempX = x;
						while (ttlBytes.Length == 0 || ttlBytes.ToString().Substring(ttlBytes.Length - 8) != "00000000")
						{
							StringBuilder columnBytes = new StringBuilder();
							for (int dy = 0; dy < 8 * GuiWidth && tempX < EndingPoint.X; dy += GuiWidth)
							{
								if (y + dy >= EndingPoint.Y)
								{
									errored = true;
									break;
								}

								Color pixel = Img[tempX, y + dy];
								columnBytes.Append(IsValidColor(pixel) || Color.White.IsRgbEqualTo(pixel) ? "1" : "0");
							}

							if (errored)
								break;

							ttlBytes.Append(columnBytes.ToString());
							tempX += GuiWidth;
						}

						if (errored)
							break;

						ttlBytes = new StringBuilder(ttlBytes.ToString().Substring(0, ttlBytes.Length - 8));

						if (BinaryToCharactersMap.ContainsKey(ttlBytes.ToString()))
						{
							realX = x;
							break;
						}
					}
				}
				// end for
				if (realX != -1 || errored)
					break;

				y += 9 * GuiWidth;
			}
			// end while

			if (realX == -1)
				throw new InvalidImageException("Couldn't find any Minecraft characters.");


			StartingPoint = new Point(realX, y);
		}

		public abstract bool IsValidColor(Color color);

		public abstract (IList<string> lobby, IDictionary<TeamColors, IList<string>> team) GetPlayerName(
			IList<string> exempt = null);

		public static bool IsInLobby(Bitmap image)
		{
			using UnmanagedImage img = UnmanagedImage.FromManagedImage(image);
			for (int y = 0; y < img.Height; y++)
			{
				for (int x = 0; x < img.Width; x++)
				{
					Color color = img.GetPixel(x, y);
					if (BossBarColor.IsRgbEqualTo(color))
					{
						return true;
					}
				}
			}

			img.Dispose();
			return false;
		}

		public void Dispose()
		{
			Img?.Dispose();
		}

		public void SaveCroppedImage(string name, int x, int y, int width, int height)
		{
			using Bitmap origImage = Img.ToManagedImage();
			using Bitmap croppedImage = origImage.Clone(new Rectangle(x, y, width, height), Img.PixelFormat);
			croppedImage.Save(name, ImageFormat.Png);
		}
	}
}