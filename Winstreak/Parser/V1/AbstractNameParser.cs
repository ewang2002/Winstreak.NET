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
		/// 
		/// </summary>
		~AbstractNameParser()
		{
			Dispose();
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
		public void InitPoints()
		{
			StartingPoint = new Point(Img.Width / 4, 20 * GuiWidth);
			EndingPoint = new Point(Img.Width - (Img.Width / 4), Img.Height / 2);
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

			for (; y <= EndingPoint.Y; y += 9 * GuiWidth)
			{
				for (int x = startX; x < endX; x++)
				{
					bool foundValidColor = false;
					for (int dy = 0; dy < 8 * GuiWidth; dy += GuiWidth)
					{
						// checking for white because
						// sometimes, when you enter lobby
						// some names will be white
						if (IsValidColor(Img[x, y + dy]) || Color.White.IsRgbEqualTo(Img[x, y + dy]) &&
							(IsValidColor(Img[x + 1, y + dy]) || Color.White.IsRgbEqualTo(Img[x + 1, y + dy]) ||
							 IsValidColor(Img[x + 2, y + dy]) || Color.White.IsRgbEqualTo(Img[x + 2, y + dy])))
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
								Color pixel = Img[tempX, y + dy];
								columnBytes.Append(IsValidColor(pixel) || Color.White.IsRgbEqualTo(pixel) ? "1" : "0");
							}

							ttlBytes.Append(columnBytes.ToString());
							tempX += GuiWidth;
						}

						ttlBytes = new StringBuilder(ttlBytes.ToString().Substring(0, ttlBytes.Length - 8));

						if (BinaryToCharactersMap.ContainsKey(ttlBytes.ToString()))
						{
							realX = x;
							break;
						}
					}
				}
				// end for
				if (realX != -1)
					break;
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