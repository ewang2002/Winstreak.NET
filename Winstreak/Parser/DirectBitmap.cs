using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Winstreak.Parser
{
	public class DirectBitmap : IDisposable
	{
		public Bitmap Bitmap { get; private set; }
		public int[] Bits { get; private set; }
		public bool Disposed { get; private set; }
		public int Height { get; private set; }
		public int Width { get; private set; }
		protected GCHandle BitsHandle { get; private set; }

		/// <summary>
		/// Initialized a DirectBitmap object.
		/// </summary>
		/// <param name="bitmap">The bitmap.</param>
		public DirectBitmap(Bitmap bitmap)
		{
			Width = bitmap.Width;
			Height = bitmap.Height;
			Bits = new int[Width * Height];
			BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
			Bitmap = new Bitmap(Width, Height, Width * 4, PixelFormat.Format32bppPArgb,
				BitsHandle.AddrOfPinnedObject());
		}

		/// <summary>
		/// Sets the pixel at a specific point. 
		/// </summary>
		/// <param name="x">The x-coordinate.</param>
		/// <param name="y">The y-coordinate.</param>
		/// <param name="color">The color.</param>
		public void SetPixel(int x, int y, Color color)
		{
			int index = x + (y * Width);
			int col = color.ToArgb();

			Bits[index] = col;
		}

		/// <summary>
		/// Gets the pixel at a specified pixel.
		/// </summary>
		/// <param name="x">The x-coordinate.</param>
		/// <param name="y">The y-coordinate.</param>
		/// <returns></returns>
		public Color GetPixel(int x, int y)
		{
			int index = x + (y * Width);
			int col = Bits[index];
			Color result = Color.FromArgb(col);

			return result;
		}

		/// <summary>
		/// Disposes the object. 
		/// </summary>
		public void Dispose()
		{
			if (Disposed)
			{
				return;
			}
			Disposed = true;
			Bitmap.Dispose();
			BitsHandle.Free();
		}
	}
}