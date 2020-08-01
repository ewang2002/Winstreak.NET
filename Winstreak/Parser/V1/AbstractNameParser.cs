using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
			//SaveCroppedImage(@"C:\Users\ewang\Desktop\A.png", StartingPoint.X, StartingPoint.Y, EndingPoint.X - StartingPoint.X, EndingPoint.Y - StartingPoint.Y);
		}

		/// <summary>
		/// Finds the x-value of the first name.
		///
		/// Precondition: There must be at least 4 names. 
		/// </summary>
		public void FindStartOfName()
		{
			int newTopLeftX = -1;
			for (int x = StartingPoint.X; x < EndingPoint.X; x++)
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
			//SaveCroppedImage(@"C:\Users\ewang\Desktop\B.png", StartingPoint.X, StartingPoint.Y, EndingPoint.X - StartingPoint.X, EndingPoint.Y - StartingPoint.Y);
		}

		public abstract bool IsValidColor(Color color);

		public abstract (IList<string> lobby, IDictionary<TeamColors, IList<string>> team) GetPlayerName(
			IList<string> exempt = null);

		public int NumberParticlesInVerticalLine(int x)
		{
			int particles = 0;
			for (int y = 0; y < Img.Height; y++)
			{
				if (IsValidColor(Img.GetPixel(x, y)))
				{
					particles++;
				}
			}

			return particles;
		}

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