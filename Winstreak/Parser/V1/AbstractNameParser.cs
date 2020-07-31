using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Winstreak.Extensions;
using Winstreak.Imaging;
using static Winstreak.Parser.Constants;

namespace Winstreak.Parser.V1
{
	public abstract class AbstractNameParser : IDisposable
	{
		// private general variables
		protected UnmanagedImage Img { get; private set; }
		public int Width { get; private set; }

		// for control
		protected bool CalledCropIfFullScreen = false;
		protected bool CalledCropHeaderFooter = false;
		protected bool CalledMakeBlkWtFunc = false;
		protected bool CalledFixImgFunc = false;

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
			this.Width = guiScale;
		}

		public abstract void CropImageIfFullScreen();

		/// <summary>
		/// Adjusts the color of the picture.
		/// </summary>
		public void AdjustColors()
		{
			if (CalledMakeBlkWtFunc)
			{
				return;
			}

			CalledMakeBlkWtFunc = true;

			// replace any invalid colors with white
			for (int y = 0; y < Img.Height; y++)
			{
				for (int x = 0; x < Img.Width; x++)
				{
					if (!IsValidColor(Img.GetPixel(x, y)))
					{
						Img.SetPixel(x, y, Color.White);
					}
				}
			}

			for (int x = 0; x < this.Img.Width; x++)
			{
				int numParticles = NumberParticlesInVerticalLine(x);
				if (numParticles > 10)
				{
					break;
				}

				for (int y = 0; y < Img.Height; y++)
				{
					if (IsValidColor(Img.GetPixel(x, y)))
					{
						Img.SetPixel(x, y, Color.White);
					}
				}
			}
		}

		/// <summary>
		/// Attempts to crop the header and footer of the image. 
		/// </summary>
		public void CropHeaderAndFooter()
		{
			if (CalledCropHeaderFooter)
			{
				return;
			}

			CalledCropHeaderFooter = true;

			bool topFirstBlankPast = false;
			bool topSepFound = false;
			int topY = -1;
			// top to bottom
			for (int y = 0; y < Img.Height; y++)
			{
				bool isSep = NumberParticlesInHorizontalLine(y) == 0;
				if (topFirstBlankPast)
				{
					if (!topSepFound && isSep)
					{
						topSepFound = true;
					}

					if (topSepFound)
					{
						if (isSep)
						{
							topY = y;
						}
						else
						{
							break;
						}
					}
				}
				else
				{
					if (!isSep)
					{
						topFirstBlankPast = true;
					}
				}
			}

			// bottom to top 
			for (int y = this.Img.Height - 1; y >= 0; y--)
			{
				bool isSep = NumberParticlesInHorizontalLine(y) == 0;
				if (isSep)
				{
					break;
				}

				for (int x = 0; x < Img.Width; x++)
				{
					if (!Img.GetPixel(x, y).IsRgbEqualTo(Color.White))
					{
						Img.SetPixel(x, y, Color.White);
					}
				}
			}

			if (topY == -1)
			{
				throw new Exception("Couldn't crop the image. Please make sure the image was processed beforehand.");
			}

			CropImage(0, topY, Img.Width, Img.Height - topY);
		}

		public abstract void FixImage();


		public void IdentifyWidth()
		{
			IDictionary<int, int> possibleWidths = new Dictionary<int, int>();
			int numWidthProcessed = 0;

			for (int y = 0; y < Img.Height; y++)
			{
				int width = 0;
				for (int x = 0; x < Img.Width; x++)
				{
					if (IsValidColor(Img.GetPixel(x, y)))
					{
						++width;
					}
					else
					{
						if (width == 0)
						{
							continue;
						}

						if (possibleWidths.ContainsKey(width))
						{
							possibleWidths[width] += 1;
						}
						else
						{
							possibleWidths.Add(width, 1);
						}

						width = 0;
						++numWidthProcessed;
					}
				}

				if (numWidthProcessed > 200)
				{
					break;
				}
			}

			KeyValuePair<int, int> maxKey = possibleWidths
				.OrderByDescending(x => x.Value)
				.First();
			Width = maxKey.Key;
		}

		public abstract bool IsValidColor(Color color);

		public int NumberParticlesInHorizontalLine(int y)
		{
			int particles = 0;
			for (int x = 0; x < Img.Width; x++)
			{
				if (IsValidColor(Img.GetPixel(x, y)))
				{
					particles++;
				}
			}

			return particles;
		}

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

		public void CropImage(int x, int y, int width, int height)
		{
			using Bitmap origImage = Img.ToManagedImage();
			using Bitmap croppedImage = origImage.Clone(new Rectangle(x, y, width, height), Img.PixelFormat);
			Img.Dispose();
			// and use new image
			Img = UnmanagedImage.FromManagedImage(croppedImage);
		}

		public void Dispose()
		{
			Img?.Dispose();
		}
	}
}