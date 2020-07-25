using System;
using System.Collections.Generic;
using System.Drawing;

namespace Winstreak.Parser.V1
{
	public abstract class AbstractNameParser
	{
		// private general variables
		protected DirectBitmap Img { get; private set;  }
		protected int Width { get; private set;  }

		// for control
		protected bool CalledCropIfFullScreen = false;
		protected bool CalledCropHeaderFooter = false;
		protected bool CalledMakeBlkWtFunc = false;
		protected bool CalledFixImgFunc = false;

		protected AbstractNameParser(Bitmap image)
		{
			this.Img = new DirectBitmap(image);
		}

		protected AbstractNameParser(string file)
		{
			this.Img = new DirectBitmap(new Bitmap(file));
		}

		public abstract void CropImageIfFullScreen();

		public void AdjustColors()
		{
			if (this.CalledMakeBlkWtFunc)
			{
				return;
			}

			this.CalledMakeBlkWtFunc = true; 

			// replace any invalid colors with white
			for (int y = 0; y < Img.Height; y++)
			{
				for (int x = 0; x < Img.Width; x++)
				{
					
				}
			}
		}

		public abstract void FixImage();
		public abstract void IdentifyWidth();
		public abstract object GetPlayerName(IList<string> exempt = null);
		public abstract bool IsValidColor(Color color);

		public void CropImage(int x, int y, int dx, int dy)
		{
			Rectangle rect = new Rectangle(x, y, dx, dy);
			Bitmap image = new Bitmap(Img.Bitmap);

			using Graphics g = Graphics.FromImage(image);
			g.DrawImage(image, x, y, rect, GraphicsUnit.Pixel);

			this.Img = new DirectBitmap(image);
		}
	}
}