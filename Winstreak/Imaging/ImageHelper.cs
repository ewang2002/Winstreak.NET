using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
	public static class ImageHelper
	{
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
		/// Load bitmap from file.
		/// </summary>
		/// 
		/// <param name="fileName">File name to load bitmap from.</param>
		/// 
		/// <returns>Returns loaded bitmap.</returns>
		/// 
		/// <remarks><para>The method is provided as an alternative of <see cref="System.Drawing.Image.FromFile(string)"/>
		/// method to solve the issues of locked file. The standard .NET's method locks the source file until
		/// image's object is disposed, so the file can not be deleted or overwritten. This method workarounds the issue and
		/// does not lock the source file.</para>
		/// 
		/// <para>Sample usage:</para>
		/// <code>
		/// Bitmap image = AForge.Imaging.Image.FromFile( "test.jpg" );
		/// </code>
		/// </remarks>
		/// 
		public static Bitmap FromFile(string fileName)
		{
			Bitmap loadedImage;
			FileStream stream = null;

			try
			{
				// read image to temporary memory stream
				stream = File.OpenRead(fileName);
				MemoryStream memoryStream = new MemoryStream();

				byte[] buffer = new byte[10000];
				while (true)
				{
					int read = stream.Read(buffer, 0, 10000);

					if (read == 0)
						break;

					memoryStream.Write(buffer, 0, read);
				}

				loadedImage = (Bitmap)System.Drawing.Image.FromStream(memoryStream);
			}
			finally
			{
				if (stream != null)
				{
					stream.Close();
					stream.Dispose();
				}
			}

			return loadedImage;
		}
	}
}