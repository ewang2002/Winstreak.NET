using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Winstreak.Extensions;
using Winstreak.External.Imaging;
using Winstreak.Parser.ImgExcept;
using static Winstreak.Parser.Constants;

namespace Winstreak.Parser.V1
{
	public abstract class AbstractNameParser : IDisposable
	{
		// private general variables
		protected UnmanagedImage Img { get; }
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
		/// The class destructor. 
		/// </summary>
		~AbstractNameParser()
		{
			Dispose();
		}

		/// <summary>
		/// Establishes the Gui scale that will be used to calculate pixels.
		/// </summary>
		/// <param name="guiScale">The Gui scale.</param>
		public void SetGuiScale(int guiScale) => 
			GuiWidth = guiScale;


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
			var y = StartingPoint.Y;
			var realX = -1;

			var startX = StartingPoint.X;
			var endX = Img.Width - startX;

			for (; y <= EndingPoint.Y; y += 9 * GuiWidth)
			{
				for (var x = startX; x < endX; x++)
				{
					var foundValidColor = false;
					for (var dy = 0; dy < 8 * GuiWidth; dy += GuiWidth)
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
						var ttlBytes = new StringBuilder();

						var tempX = x;
						while (ttlBytes.Length == 0 || ttlBytes.ToString().Substring(ttlBytes.Length - 8) != "00000000")
						{
							var columnBytes = new StringBuilder();
							for (var dy = 0; dy < 8 * GuiWidth && tempX < EndingPoint.X; dy += GuiWidth)
							{
								var pixel = Img[tempX, y + dy];
								columnBytes.Append(IsValidColor(pixel) || Color.White.IsRgbEqualTo(pixel) ? "1" : "0");
							}

							ttlBytes.Append(columnBytes.ToString());
							tempX += GuiWidth;
						}

						ttlBytes = new StringBuilder(ttlBytes.ToString().Substring(0, ttlBytes.Length - 8));

						if (!BinaryToCharactersMap.ContainsKey(ttlBytes.ToString()))
							continue;
						realX = x;
						break;
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

		/// <summary>
		/// Determines if a color is a valid color. This method needs to be implemented in any subclasses.
		/// </summary>
		/// <param name="color">The color.</param>
		/// <returns>Whether the color is a valid color in the context of the subclass.</returns>
		public abstract bool IsValidColor(Color color);

		/// <summary>
		/// Parses the player's name from a screenshot. 
		/// </summary>
		/// <param name="exempt">Any names that shouldn't be included in the parsing results.</param>
		/// <returns>A tuple containing a list or a dictionary. Depending on the needs of the subclass, one of the two tuple elements will actually have the parsed names.</returns>
		public abstract (IList<string> lobby, IDictionary<TeamColors, IList<string>> team) GetPlayerName(
			IList<string> exempt = null);

		/// <summary>
		/// Determines if a screenshot was taken in the lobby or in-game.
		/// </summary>
		/// <param name="image">The image.</param>
		/// <returns>Whether the screenshot was taken in the lobby.</returns>
		public static bool IsInLobby(Bitmap image)
		{
			using var img = UnmanagedImage.FromManagedImage(image);
			for (var y = 0; y < img.Height; y++)
			{
				for (var x = 0; x < img.Width; x++)
				{
					var color = img.GetPixel(x, y);
					if (BossBarColor.IsRgbEqualTo(color))
					{
						return true;
					}
				}
			}

			img.Dispose();
			return false;
		}

		/// <summary>
		/// Disposes the image.
		/// </summary>
		public void Dispose()
		{
			Img?.Dispose();
		}
	}
}