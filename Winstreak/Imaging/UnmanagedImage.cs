// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2005-2012
// contacts@aforgenet.com
//
// Accord Imaging Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

// TAKEN FROM https://github.com/accord-net/framework/blob/development/Sources/Accord.Imaging/AForge.Imaging/UnmanagedImage.cs
// Due to use of outdated libraries. 

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Winstreak.Parser.ImgExcept;

namespace Winstreak.Imaging
{
	/// <summary>
	/// Image in unmanaged memory.
	/// </summary>
	/// 
	/// <remarks>
	/// <para>The class represents wrapper of an image in unmanaged memory. Using this class
	/// it is possible as to allocate new image in unmanaged memory, as to just wrap provided
	/// pointer to unmanaged memory, where an image is stored.</para>
	/// 
	/// <para>Usage of unmanaged images is mostly beneficial when it is required to apply <b>multiple</b>
	/// image processing routines to a single image. In such scenario usage of .NET managed images 
	/// usually leads to worse performance, because each routine needs to lock managed image
	/// before image processing is done and then unlock it after image processing is done. Without
	/// these lock/unlock there is no way to get direct access to managed image's data, which means
	/// there is no way to do fast image processing. So, usage of managed images lead to overhead, which
	/// is caused by locks/unlock. Unmanaged images are represented internally using unmanaged memory
	/// buffer. This means that it is not required to do any locks/unlocks in order to get access to image
	/// data (no overhead).</para>
	/// 
	/// <para>Sample usage:</para>
	/// <code>
	/// // sample 1 - wrapping .NET image into unmanaged without
	/// // making extra copy of image in memory
	/// BitmapData imageData = image.LockBits(
	///     new Rectangle( 0, 0, image.Width, image.Height ),
	///     ImageLockMode.ReadWrite, image.PixelFormat );
	/// 
	/// try
	/// {
	///     UnmanagedImage unmanagedImage = new UnmanagedImage( imageData ) );
	///     // apply several routines to the unmanaged image
	/// }
	/// finally
	/// {
	///     image.UnlockBits( imageData );
	/// }
	/// 
	/// 
	/// // sample 2 - converting .NET image into unmanaged
	/// UnmanagedImage unmanagedImage = UnmanagedImage.FromManagedImage( image );
	/// // apply several routines to the unmanaged image
	/// ...
	/// // conver to managed image if it is required to display it at some point of time
	/// Bitmap managedImage = unmanagedImage.ToManagedImage( );
	/// </code>
	/// </remarks>
	/// 
	public class UnmanagedImage : IDisposable
	{
		// flag which indicates if the image should be disposed or not
		private bool _mustBeDisposed;


		/// <summary>
		/// Pointer to image data in unmanaged memory.
		/// </summary>
		public IntPtr ImageData { get; private set; }

		/// <summary>
		/// Image width in pixels.
		/// </summary>
		public int Width { get; private set; }

		/// <summary>
		/// Image height in pixels.
		/// </summary>
		public int Height { get; private set; }

		/// <summary>
		/// Image stride (line size in bytes).
		/// </summary>
		public int Stride { get; private set; }

		/// <summary>
		/// Image pixel format.
		/// </summary>
		public PixelFormat PixelFormat { get; private set; }

		/// <summary>
		/// Gets the image size, in bytes.
		/// </summary>
		/// 
		public int NumberOfBytes
		{
			get { return Stride * Height; }
		}

		/// <summary>
		/// Gets the image size, in pixels.
		/// </summary>
		/// 
		public int Size => Width * Height;

		/// <summary>
		/// Gets the number of extra bytes after the image width is over. This can be computed
		/// as <see cref="Stride"/> - <see cref="Width"/> * <see cref="PixelSize"/>.
		/// </summary>
		/// 
		public int Offset => Stride - Width * PixelSize;

		/// <summary>
		/// Gets the size of the pixels in this image, in bytes. For 
		/// example, a 8-bpp grayscale image would have pixel size 1.
		/// </summary>
		/// 
		public int PixelSize => Image.GetPixelFormatSize(PixelFormat) / 8;

		/// <summary>
		/// Initializes a new instance of the <see cref="UnmanagedImage"/> class.
		/// </summary>
		/// 
		/// <param name="imageData">Pointer to image data in unmanaged memory.</param>
		/// <param name="width">Image width in pixels.</param>
		/// <param name="height">Image height in pixels.</param>
		/// <param name="stride">Image stride (line size in bytes).</param>
		/// <param name="pixelFormat">Image pixel format.</param>
		/// 
		/// <remarks><para><note>Using this constructor, make sure all specified image attributes are correct
		/// and correspond to unmanaged memory buffer. If some attributes are specified incorrectly,
		/// this may lead to exceptions working with the unmanaged memory.</note></para></remarks>
		/// 
		public UnmanagedImage(IntPtr imageData, int width, int height, int stride, PixelFormat pixelFormat)
			=> Init(imageData, width, height, stride, pixelFormat);


		/// <summary>
		/// Initializes a new instance of the <see cref="UnmanagedImage"/> class.
		/// </summary>
		/// 
		/// <param name="bitmapData">Locked bitmap data.</param>
		/// 
		/// <remarks><note>Unlike <see cref="FromManagedImage(BitmapData)"/> method, this constructor does not make
		/// copy of managed image. This means that managed image must stay locked for the time of using the instance
		/// of unamanged image.</note></remarks>
		/// 
		public UnmanagedImage(BitmapData bitmapData)
			=> Init(bitmapData.Scan0, bitmapData.Width, bitmapData.Height, bitmapData.Stride, bitmapData.PixelFormat);


		private void Init(IntPtr imageData, int width, int height, int stride, PixelFormat pixelFormat)
		{
			ImageData = imageData;
			Width = width;
			Height = height;
			Stride = stride;
			PixelFormat = pixelFormat;
		}

		/// <summary>
		/// Destroys the instance of the <see cref="UnmanagedImage"/> class.
		/// </summary>
		/// 
		~UnmanagedImage()
			=> Dispose(false);


		/// <summary>
		/// Dispose the object.
		/// </summary>
		/// <remarks>Frees unmanaged resources used by the object. The object becomes unusable
		///     after that.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// remove me from the Finalization queue 
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose the object.
		/// </summary>
		/// 
		/// <param name="disposing">Indicates if disposing was initiated manually.</param>
		/// 
		protected virtual void Dispose(bool disposing)
		{
			// free image memory if the image was allocated using this class
			if (!_mustBeDisposed || ImageData == IntPtr.Zero)
				return;


			Marshal.FreeHGlobal(ImageData);
			GC.RemoveMemoryPressure(Stride * Height);
			ImageData = IntPtr.Zero;
		}

		/// <summary>
		/// Create managed image from the unmanaged.
		/// </summary>
		/// 
		/// <returns>Returns managed copy of the unmanaged image.</returns>
		/// 
		/// <remarks><para>The method creates a managed copy of the unmanaged image with the
		/// same size and pixel format (it calls <see cref="ToManagedImage(bool)"/> specifying
		/// <see langword="true"/> for the <b>makeCopy</b> parameter).</para></remarks>
		/// 
		public Bitmap ToManagedImage()
			=> ToManagedImage(true);


		/// <summary>
		/// Create managed image from the unmanaged.
		/// </summary>
		/// 
		/// <param name="makeCopy">Make a copy of the unmanaged image or not.</param>
		/// 
		/// <returns>Returns managed copy of the unmanaged image.</returns>
		/// 
		/// <remarks><para>If the <paramref name="makeCopy"/> is set to <see langword="true"/>, then the method
		/// creates a managed copy of the unmanaged image, so the managed image stays valid even when the unmanaged
		/// image gets disposed. However, setting this parameter to <see langword="false"/> creates a managed image which is
		/// just a wrapper around the unmanaged image. So if unmanaged image is disposed, the
		/// managed image becomes no longer valid and accessing it will generate an exception.</para></remarks>
		/// 
		/// <exception cref="InvalidImageException">The unmanaged image has some invalid properties, which results
		/// in failure of converting it to managed image.</exception>
		public Bitmap ToManagedImage(bool makeCopy)
		{
			Bitmap dstImage = null;

			try
			{
				if (!makeCopy)
				{
					dstImage = new Bitmap(Width, Height, Stride, PixelFormat, ImageData);
					if (PixelFormat == PixelFormat.Format8bppIndexed)
					{
						dstImage.SetGrayscalePalette();
					}
				}
				else
				{
					// create new image of required format
					dstImage = (PixelFormat == PixelFormat.Format8bppIndexed)
						? ImageHelper.CreateGrayscaleImage(Width, Height)
						: new Bitmap(Width, Height, PixelFormat);

					// lock destination bitmap data
					var dstData = dstImage.LockBits(
						new Rectangle(0, 0, Width, Height),
						ImageLockMode.ReadWrite, dstImage.PixelFormat);

					var dstStride = dstData.Stride;
					var lineSize = Math.Min(Stride, dstStride);

					unsafe
					{
						var dst = (byte*) dstData.Scan0.ToPointer();
						var src = (byte*) ImageData.ToPointer();

						if (Stride != dstStride)
							// copy image
							for (var y = 0; y < Height; y++)
							{
								SystemTools.CopyUnmanagedMemory(dst, src, lineSize);
								dst += dstStride;
								src += Stride;
							}
						else
							SystemTools.CopyUnmanagedMemory(dst, src, Stride * Height);
					}

					// unlock destination images
					dstImage.UnlockBits(dstData);
				}

				return dstImage;
			}
			catch (Exception)
			{
				dstImage?.Dispose();
				throw new InvalidImageException(
					"The unmanaged image has some invalid properties, which results in failure of converting it to managed image.");
			}
		}

		/// <summary>
		/// Create unmanaged image from the specified managed image.
		/// </summary>
		/// 
		/// <param name="image">Source managed image.</param>
		/// 
		/// <returns>Returns new unmanaged image, which is a copy of source managed image.</returns>
		/// 
		/// <remarks><para>The method creates an exact copy of specified managed image, but allocated
		/// in unmanaged memory.</para></remarks>
		/// 
		/// <exception cref="InvalidImageException">Unsupported pixel format of source image.</exception>
		public static UnmanagedImage FromManagedImage(Bitmap image)
		{
			UnmanagedImage dstImage;

			var sourceData = image.LockBits(
				new Rectangle(0, 0, image.Width, image.Height),
				ImageLockMode.ReadOnly, image.PixelFormat);

			try
			{
				dstImage = FromManagedImage(sourceData);
			}
			finally
			{
				image.UnlockBits(sourceData);
			}

			return dstImage;
		}

		/// <summary>
		/// Create unmanaged image from the specified managed image.
		/// </summary>
		/// 
		/// <param name="imageData">Source locked image data.</param>
		/// 
		/// <returns>Returns new unmanaged image, which is a copy of source managed image.</returns>
		/// 
		/// <remarks><para>The method creates an exact copy of specified managed image, but allocated
		/// in unmanaged memory. This means that managed image may be unlocked right after call to this
		/// method.</para></remarks>
		/// 
		/// <exception cref="InvalidImageException">Unsupported pixel format of source image.</exception>
		public static UnmanagedImage FromManagedImage(BitmapData imageData)
		{
			var pixelFormat = imageData.PixelFormat;

			// check source pixel format
			if (pixelFormat != PixelFormat.Format8bppIndexed &&
			    pixelFormat != PixelFormat.Format16bppGrayScale &&
			    pixelFormat != PixelFormat.Format24bppRgb &&
			    pixelFormat != PixelFormat.Format32bppRgb &&
			    pixelFormat != PixelFormat.Format32bppArgb &&
			    pixelFormat != PixelFormat.Format32bppPArgb &&
			    pixelFormat != PixelFormat.Format48bppRgb &&
			    pixelFormat != PixelFormat.Format64bppArgb &&
			    pixelFormat != PixelFormat.Format64bppPArgb)
				throw new InvalidImageException("Unsupported pixel format of the source image.");

			return FromUnmanagedData(imageData.Scan0, imageData.Width, imageData.Height, imageData.Stride, pixelFormat);
		}

		private static UnmanagedImage FromUnmanagedData(IntPtr imageData, int width, int height, int stride,
			PixelFormat pixelFormat)
		{
			// allocate memory for the image
			var dstImageData = Marshal.AllocHGlobal(stride * height);
			GC.AddMemoryPressure(stride * height);

			var image = new UnmanagedImage(dstImageData, width, height, stride, pixelFormat);
			SystemTools.CopyUnmanagedMemory(dstImageData, imageData, stride * height);
			image._mustBeDisposed = true;

			return image;
		}


		/// <summary>
		/// Set pixel with the specified coordinates to the specified color.
		/// </summary>
		/// 
		/// <param name="x">X coordinate of the pixel to set.</param>
		/// <param name="y">Y coordinate of the pixel to set.</param>
		/// <param name="color">Color to set for the pixel.</param>
		/// 
		/// <remarks><para><note>For images having 16 bpp per color plane, the method extends the specified color
		/// value to 16 bit by multiplying it by 256.</note></para>
		/// 
		/// <para>For grayscale images this method will calculate intensity value based on the below formula:
		/// <code lang="none">
		/// 0.2125 * Red + 0.7154 * Green + 0.0721 * Blue
		/// </code>
		/// </para>
		/// </remarks>
		public void SetPixel(int x, int y, Color color)
			=> SetPixel(x, y, color.R, color.G, color.B, color.A);
		

		private void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
		{
			if (x >= 0 && y >= 0 && x < Width && y < Height)
			{
				unsafe
				{
					var pixelSize = Image.GetPixelFormatSize(PixelFormat) / 8;
					var ptr = (byte*) ImageData.ToPointer() + y * Stride + x * pixelSize;
					var ptr2 = (ushort*) ptr;

					switch (PixelFormat)
					{
						case PixelFormat.Format8bppIndexed:
							*ptr = (byte) (0.2125 * r + 0.7154 * g + 0.0721 * b);
							break;

						case PixelFormat.Format24bppRgb:
						case PixelFormat.Format32bppRgb:
							ptr[Rgb.R] = r;
							ptr[Rgb.G] = g;
							ptr[Rgb.B] = b;
							break;

						case PixelFormat.Format32bppArgb:
							ptr[Rgb.R] = r;
							ptr[Rgb.G] = g;
							ptr[Rgb.B] = b;
							ptr[Rgb.A] = a;
							break;

						case PixelFormat.Format16bppGrayScale:
							*ptr2 = (ushort) ((ushort) (0.2125 * r + 0.7154 * g + 0.0721 * b) << 8);
							break;

						case PixelFormat.Format48bppRgb:
							ptr2[Rgb.R] = (ushort) (r << 8);
							ptr2[Rgb.G] = (ushort) (g << 8);
							ptr2[Rgb.B] = (ushort) (b << 8);
							break;

						case PixelFormat.Format64bppArgb:
							ptr2[Rgb.R] = (ushort) (r << 8);
							ptr2[Rgb.G] = (ushort) (g << 8);
							ptr2[Rgb.B] = (ushort) (b << 8);
							ptr2[Rgb.A] = (ushort) (a << 8);
							break;

						default:
							throw new InvalidImageException(
								"The pixel format is not supported: " + PixelFormat);
					}
				}
			}
		}

		/// <summary>
		/// Get color of the pixel with the specified coordinates.
		/// </summary>
		/// 
		/// <param name="x">X coordinate of the pixel to get.</param>
		/// <param name="y">Y coordinate of the pixel to get.</param>
		/// 
		/// <returns>Return pixel's color at the specified coordinates.</returns>
		/// 
		/// <remarks>
		/// <para><note>In the case if the image has 8 bpp grayscale format, the method will return a color with
		/// all R/G/B components set to same value, which is grayscale intensity.</note></para>
		/// 
		/// <para><note>The method supports only 8 bpp grayscale images and 24/32 bpp color images so far.</note></para>
		/// </remarks>
		/// 
		/// <exception cref="ArgumentOutOfRangeException">The specified pixel coordinate is out of image's bounds.</exception>
		/// <exception cref="InvalidImageException">Pixel format of this image is not supported by the method.</exception>
		public Color GetPixel(int x, int y)
		{
			if (x < 0 || y < 0)
				throw new ArgumentOutOfRangeException(nameof(x),
					"The specified pixel coordinate is out of image's bounds.");
			

			if (x >= Width || y >= Height)
				throw new ArgumentOutOfRangeException(nameof(y),
					"The specified pixel coordinate is out of image's bounds.");
			

			Color color;

			unsafe
			{
				var pixelSize = PixelFormat.GetPixelFormatSize() / 8;
				var ptr = (byte*) ImageData.ToPointer() + y * Stride + x * pixelSize;

				color = PixelFormat switch
				{
					PixelFormat.Format8bppIndexed => Color.FromArgb(*ptr, *ptr, *ptr),
					PixelFormat.Format24bppRgb => Color.FromArgb(ptr[Rgb.R], ptr[Rgb.G], ptr[Rgb.B]),
					PixelFormat.Format32bppRgb => Color.FromArgb(ptr[Rgb.R], ptr[Rgb.G], ptr[Rgb.B]),
					PixelFormat.Format32bppArgb => Color.FromArgb(ptr[Rgb.A], ptr[Rgb.R], ptr[Rgb.G], ptr[Rgb.B]),
					_ => throw new InvalidImageException("The pixel format is not supported: " + PixelFormat)
				};
			}

			return color;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public Color this[int x, int y]
		{
			get => GetPixel(x, y);
			set => SetPixel(x, y, value);
		}
	}
}