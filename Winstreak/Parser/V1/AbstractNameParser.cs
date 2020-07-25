using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Winstreak.Parser.ImgExcept;

namespace Winstreak.Parser.V1
{
	public abstract class AbstractNameParser
	{
		// private general variables
		protected DirectBitmap Img { get; private set; }
		protected int Width { get; private set; }

		// for control
		protected bool CalledCropIfFullScreen = false;
		protected bool CalledCropHeaderFooter = false;
		protected bool CalledMakeBlkWtFunc = false;
		protected bool CalledFixImgFunc = false;

		protected AbstractNameParser(Bitmap image)
		{
			Img = new DirectBitmap(image);
		}

		protected AbstractNameParser(string file)
		{
			Img = new DirectBitmap(new Bitmap(file));
		}

		public abstract void CropImageIfFullScreen();

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

			for (int y = this.Img.Height - 1; y >= 0; y--)
			{
				bool isSep = NumberParticlesInHorizontalLine(y) == 0;
				if (isSep)
				{
					break;
				}

				for (int x = 0; x < Img.Width; x++)
				{
					if (Img.GetPixel(x, y) != Color.White)
					{
						Img.SetPixel(x, y, Color.White);
					}
				}
			}

			if (topY == -1)
			{
				throw new InvalidImageException("Couldn't crop the image. Please make sure the image was processed beforehand.");
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
				.OrderBy(x => x.Value)
				.First();
			Width = maxKey.Key;
		}

		public abstract bool IsValidColor(Color color);

		public void CropImage(int x, int y, int dx, int dy)
		{
			Rectangle rect = new Rectangle(x, y, dx, dy);
			Bitmap image = new Bitmap(Img.Bitmap);

			using Graphics g = Graphics.FromImage(image);
			g.DrawImage(image, x, y, rect, GraphicsUnit.Pixel);

			Img = new DirectBitmap(image);
		}

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
	}
}