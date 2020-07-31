using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using Winstreak.Parser.ImgExcept;

namespace Winstreak.Imaging
{
	/// <summary>
	/// Core image relatad methods.
	/// </summary>
	/// 
	/// <remarks>All methods of this class are static and represent general routines
	/// used by different image processing classes.</remarks>
	/// 
	public static class Image
	{
		/// <summary>
		/// Check if specified 8 bpp image is grayscale.
		/// </summary>
		/// 
		/// <param name="image">Image to check.</param>
		/// 
		/// <returns>Returns <b>true</b> if the image is grayscale or <b>false</b> otherwise.</returns>
		/// 
		/// <remarks>The methods checks if the image is a grayscale image of 256 gradients.
		/// The method first examines if the image's pixel format is
		/// <see cref="System.Drawing.Imaging.PixelFormat">Format8bppIndexed</see>
		/// and then it examines its palette to check if the image is grayscale or not.</remarks>
		/// 
		/// <seealso cref="IsColor8bpp(Bitmap)"/>
		/// 
		public static bool IsGrayscale(this Bitmap image)
		{
			// check pixel format
			if (image.PixelFormat == PixelFormat.Format8bppIndexed)
			{
				// check palette
				ColorPalette cp = image.Palette;

				// init palette
				for (int i = 0; i < 256; i++)
				{
					Color c = cp.Entries[i];
					if ((c.R != i) || (c.G != i) || (c.B != i))
						return false;
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Create and initialize new 8 bpp grayscale image.
		/// </summary>
		/// 
		/// <param name="width">Image width.</param>
		/// <param name="height">Image height.</param>
		/// 
		/// <returns>Returns the created grayscale image.</returns>
		/// 
		/// <remarks>The method creates new 8 bpp grayscale image and initializes its palette.
		/// Grayscale image is represented as
		/// <see cref="System.Drawing.Imaging.PixelFormat">Format8bppIndexed</see>
		/// image with palette initialized to 256 gradients of gray color.</remarks>
		/// 
		public static Bitmap CreateGrayscaleImage(int width, int height)
		{
			// create new image
			Bitmap image = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

			// set palette to grayscale
			SetGrayscalePalette(image);

			// return new image
			return image;
		}

		/// <summary>
		/// Set pallete of the 8 bpp indexed image to grayscale.
		/// </summary>
		/// 
		/// <param name="image">Image to initialize.</param>
		/// 
		/// <remarks>The method initializes palette of
		/// <see cref="System.Drawing.Imaging.PixelFormat">Format8bppIndexed</see>
		/// image with 256 gradients of gray color.</remarks>
		/// 
		/// <exception cref="UnsupportedImageFormatException">Provided image is not 8 bpp indexed image.</exception>
		/// 
		public static void SetGrayscalePalette(this Bitmap image)
		{
			// check pixel format
			if (image.PixelFormat != PixelFormat.Format8bppIndexed)
				throw new InvalidImageException("Source image is not 8 bpp image.");

			// get palette
			ColorPalette cp = image.Palette;

			// init palette
			for (int i = 0; i < 256; i++)
				cp.Entries[i] = Color.FromArgb(i, i, i);

			// set palette back
			image.Palette = cp;
		}

		/// <summary>
		/// Copy image.
		/// </summary>
		/// 
		/// <param name="source">Source image.</param>
		/// <param name="destination">Destination image. If set to null, a new image will be created.</param>
		/// 
		/// <returns>Returns clone of the source image with specified pixel format.</returns>
		///
		public static Bitmap Copy(this Bitmap source, Bitmap destination)
		{
			int width = source.Width;
			int height = source.Height;

			if (destination == null)
				destination = new Bitmap(width, height, source.PixelFormat);

			// draw source image on the new one using Graphics
			using (Graphics g = Graphics.FromImage(destination))
				g.DrawImage(source, 0, 0, width, height);

			return destination;
		}


		/// <summary>
		/// Clone image.
		/// </summary>
		/// 
		/// <param name="sourceData">Source image data.</param>
		///
		/// <returns>Clones image from source image data. The message does not clone pallete in the
		/// case if the source image has indexed pixel format.</returns>
		/// 
		public static Bitmap Clone(this BitmapData sourceData)
		{
			return Copy(sourceData, null);
		}

		private static Bitmap Copy(this BitmapData sourceData, Bitmap destination)
		{
			if (destination == null)
			{
				// create new image
				destination = new Bitmap(sourceData.Width, sourceData.Height, sourceData.PixelFormat);
			}

			// get source image size
			int width = sourceData.Width;
			int height = sourceData.Height;

			// lock destination bitmap data
			BitmapData destinationData = destination.LockBits(
				new Rectangle(0, 0, width, height),
				ImageLockMode.ReadWrite, destination.PixelFormat);

			System.Diagnostics.Debug.Assert(destinationData.Stride == sourceData.Stride);

			SystemTools.CopyUnmanagedMemory(destinationData.Scan0, sourceData.Scan0, height * sourceData.Stride);

			// unlock destination image
			destination.UnlockBits(destinationData);

			return destination;
		}

		/// <summary>
		/// Gets the color depth used in a pixel format, in number of bytes per pixel.
		/// </summary>
		/// <param name="format">The pixel format.</param>
		public static int GetPixelFormatSizeInBytes(this PixelFormat format)
		{
			return System.Drawing.Image.GetPixelFormatSize(format) / 8;
		}

		/// <summary>
		/// Gets the color depth used in a pixel format, in number of bits per pixel.
		/// </summary>
		/// <param name="format">The pixel format.</param>
		public static int GetPixelFormatSize(this PixelFormat format)
		{
			return System.Drawing.Image.GetPixelFormatSize(format);
		}

		/// <summary>
		/// Gets the color depth used in an image, in number of bytes per pixel.
		/// </summary>
		/// <param name="image">The image.</param>
		public static int GetPixelFormatSizeInBytes(this Bitmap image)
		{
			return image.PixelFormat.GetPixelFormatSizeInBytes();
		}
	}
}