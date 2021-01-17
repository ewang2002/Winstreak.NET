// Accord Imaging Library
// The Accord.NET Framework
// http://accord-framework.net
//
// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2007-2011
// contacts@aforgenet.com
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
//

// TAKEN FROM https://github.com/accord-net/framework/blob/792015d0e2ee250228dfafb99ea0e84d031a29ae/Sources/Accord.Imaging/Colors/RGB.cs

using System;
using System.Drawing;

namespace Winstreak.Core.Parsers.ImageParser.Imaging
{
	/// <summary>
	///   RGB components.
	/// </summary>
	/// 
	/// <remarks>
	/// <para>
	///   The class encapsulates <b>RGB</b> color components and can be used to implement
	///   logic for reading, writing and converting to and from RGB color representations.</para>
	/// <para>
	///   <note>The <see cref="System.Drawing.Imaging.PixelFormat">PixelFormat.Format24bppRgb</see>
	///   actually refers to a BGR pixel format.</note></para>
	/// </remarks>
	[Serializable]
	public struct Rgb
	{
		/// <summary>
		/// Index of red component.
		/// </summary>
		public const short R = 2;

		/// <summary>
		/// Index of green component.
		/// </summary>
		public const short G = 1;

		/// <summary>
		/// Index of blue component.
		/// </summary>
		public const short B = 0;

		/// <summary>
		/// Index of alpha component for ARGB images.
		/// </summary>
		public const short A = 3;

		/// <summary>
		/// Red component.
		/// </summary>
		public byte Red;

		/// <summary>
		/// Green component.
		/// </summary>
		public byte Green;

		/// <summary>
		/// Blue component.
		/// </summary>
		public byte Blue;

		/// <summary>
		/// Alpha component.
		/// </summary>
		public byte Alpha;

		/// <summary>
		/// <see cref="System.Drawing.Color">Color</see> value of the class.
		/// </summary>
		public Color Color
		{
			get => Color.FromArgb(Alpha, Red, Green, Blue);
			set
			{
				Red = value.R;
				Green = value.G;
				Blue = value.B;
				Alpha = value.A;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Rgb"/> class.
		/// </summary>
		/// 
		/// <param name="red">Red component.</param>
		/// <param name="green">Green component.</param>
		/// <param name="blue">Blue component.</param>
		public Rgb(byte red, byte green, byte blue)
		{
			Red = red;
			Green = green;
			Blue = blue;
			Alpha = 255;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Rgb"/> class.
		/// </summary>
		/// 
		/// <param name="red">Red component.</param>
		/// <param name="green">Green component.</param>
		/// <param name="blue">Blue component.</param>
		/// <param name="alpha">Alpha component.</param>
		public Rgb(byte red, byte green, byte blue, byte alpha)
		{
			Red = red;
			Green = green;
			Blue = blue;
			Alpha = alpha;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Rgb"/> class.
		/// </summary>
		/// 
		/// <param name="color">Initialize from specified <see cref="System.Drawing.Color">color.</see></param>
		public Rgb(Color color)
		{
			Red = color.R;
			Green = color.G;
			Blue = color.B;
			Alpha = color.A;
		}
	}
}