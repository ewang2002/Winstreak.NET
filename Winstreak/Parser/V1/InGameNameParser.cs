using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Winstreak.Parser.ImgExcept;
using static Winstreak.Parser.Constants;

namespace Winstreak.Parser.V1
{
	public class InGameNameParser : AbstractNameParser
	{
		public InGameNameParser(Bitmap image) : base(image)
		{
		}

		public InGameNameParser(string file) : base(file)
		{
		}

		public override void CropImageIfFullScreen()
		{
			if (base.CalledCropIfFullScreen)
			{
				return;
			}

			base.CalledCropIfFullScreen = true;

			// top to bottom, left to right
			// find the top left coordinates of the
			// player list box
			int topLeftX = -1;
			int topLeftY = -1;

			for (int y = 0; y < base.Img.Height; y++)
			{
				bool canBreak = false;
				for (int x = 0; x < base.Img.Width; x++)
				{
					if (base.Img.GetPixel(x, y) == BossBarColor)
					{
						topLeftX = x;
						topLeftY = y;
						canBreak = true;
						break;
					}
				}

				if (canBreak)
				{
					break;
				}
			}

			// right to left, bottom to top
			int bottomRightX = -1;
			int bottomRightY = -1;

			for (int x = base.Img.Width - ListedNumsOffset; x >= 0; x--)
			{
				bool canBreak = false;
				for (int y = base.Img.Height - 1; y >= 0; y--)
				{
					if (base.Img.GetPixel(x, y) == StoreHypixelNetDarkColor)
					{
						bottomRightX = x;
						bottomRightY = y;
						canBreak = true;
						break;
					}
				}

				if (canBreak)
				{
					break;
				}
			}

			if (topLeftX == -1 || topLeftY == -1)
			{
				throw new InvalidImageException(
					"Invalid image given. Either a player list wasn't detected or the \"background\" of the player list isn't just the sky. Make sure the image contains the player list and that the \"background\" of the player list is just the sky (no clouds).");
			}

			base.CropImage(topLeftX, topLeftY, bottomRightX - topLeftX, bottomRightY - topLeftY);
		}

		public override void FixImage()
		{
			if (base.CalledMakeBlkWtFunc)
			{
				return;
			}

			base.CalledFixImgFunc = true;
			int startingXVal;
			int startingYVal;
			int minStartingXVal = base.Img.Width;
			int minStartingYVal = base.Img.Height;
			for (int y = 0; y < base.Img.Height; y++)
			{
				for (int x = 0; x < base.Img.Width; x++)
				{
					if (IsValidColor(base.Img.GetPixel(x, y)))
					{
						if (x < minStartingXVal)
						{
							minStartingXVal = x;
						}

						if (y < minStartingYVal)
						{
							minStartingYVal = y;
						}
					}
				}
			}

			startingXVal = minStartingXVal;
			startingYVal = minStartingYVal;

			if (startingXVal == base.Img.Width || startingYVal == base.Img.Height)
			{
				throw new InvalidImageException(
					"Couldn't crop the image. Make sure the image was processed beforehand.");
			}

			base.CropImage(startingXVal, startingYVal, base.Img.Width - startingXVal, base.Img.Height - startingYVal);
		}

		public IList<string> GetPlayerName(IList<string> exempt = null)
		{
			if (!base.CalledMakeBlkWtFunc && !base.CalledFixImgFunc)
			{
				return new List<string>();
			}

			exempt ??= new List<string>();

			IList<string> names = new List<string>();
			int y = 0;

			while (y <= base.Img.Height)
			{
				StringBuilder name = new StringBuilder();
				int x = 0;

				while (true)
				{
					StringBuilder ttlBytes = new StringBuilder();
					bool errored = false;

					while (ttlBytes.Length == 0 || ttlBytes.ToString()[(ttlBytes.Length - 8)..] == "00000000")
					{
						try
						{
							StringBuilder columnBytes = new StringBuilder();
							for (int dy = 0; dy < 8 * base.Width; dy += base.Width)
							{
								if (IsValidColor(base.Img.GetPixel(x, y)))
								{
									columnBytes.Append("1");
								}
								else
								{
									columnBytes.Append("0");
								}
							}

							ttlBytes.Append(columnBytes.ToString());
							x += base.Width;
						}
						catch (Exception)
						{
							errored = true;
							break;
						}
					}

					if (!errored)
					{
						// ttlBytes = new StringBuilder(ttlBytes.substring(0, ttlBytes.length() - 8));
						ttlBytes = new StringBuilder(ttlBytes.ToString()[0..^8]);
					}

					if (BinaryToCharactersMap.ContainsKey(ttlBytes.ToString()))
					{
						name.Append(BinaryToCharactersMap[ttlBytes.ToString()]);
					}
					else
					{
						break;
					}
				}

				if (!exempt.Contains(name.ToString()))
				{
					names.Add(name.ToString());
				}

				y += 9 * base.Width;
			}

			names = names
				.Where(x => x.Length != 0)
				.ToList();

			return names; 
		}

		public override bool IsValidColor(Color color)
		{
			throw new NotImplementedException();
		}
	}
}