using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Winstreak.Extensions;
using Winstreak.Imaging;
using Winstreak.Parser.ImgExcept;
using static Winstreak.Parser.Constants;

namespace Winstreak.Parser
{
	/// <summary>
	/// Parses a screenshot containing the member tab list.
	/// </summary>
	public class NameParser : IDisposable
	{
		/// <summary>
		/// The image.
		/// </summary>
		public UnmanagedImage Img { get; }

		/// <summary>
		/// Minecraft's GUI width.
		/// </summary>
		public int GuiWidth { get; private set; }

		/// <summary>
		/// The starting point; i.e., where the first name is located.
		/// </summary>
		public Point StartingPoint { get; private set; }

		/// <summary>
		/// The ending point.
		/// </summary>
		public Point EndingPoint { get; private set; }

		/// <summary>
		/// Whether the screenshot represents a lobby.
		/// </summary>
		public bool IsLobby { get; private set; } = true;

		/// <summary>
		/// Instantiates a new NameParser object with the specified Bitmap.
		/// </summary>
		/// <param name="image">The bitmap.</param>
		public NameParser(Bitmap image) => Img = UnmanagedImage.FromManagedImage(image);

		/// <summary>
		/// Sets the Gui scale.
		/// </summary>
		/// <param name="scale">The scale.</param>
		public void SetGuiScale(int scale) => GuiWidth = scale;

		/// <summary>
		/// Finds the starting and ending point of the image. 
		/// </summary>
		public void InitPoints()
		{
			StartingPoint = new Point(Img.Width / 4, 20 * GuiWidth);
			EndingPoint = new Point(Img.Width - (Img.Width / 4), Img.Height / 2);
		}

		/// <summary>
		/// Finds the start of the name.
		/// </summary>
		/// <returns></returns>
		public void FindStartOfName()
		{
			var y = StartingPoint.Y;
			var realX = -1;

			var startX = StartingPoint.X;
			var endX = Img.Width - startX;

			var inGame = false;
			for (; y <= EndingPoint.Y; y += 9 * GuiWidth)
			{
				for (var x = startX; x < endX; x++)
				{
					var foundValidColor = false;
					for (var dy = 0; dy < 8 * GuiWidth; dy += GuiWidth)
					{
						var p0 = Img[x, y + dy];
						var p1 = Img[x + 1, y + dy];
						var p2 = Img[x + 2, y + dy];
						if (!IsValidRankColor(p0)
						    && !IsTeamColor(p0)
						    && (!Color.White.IsRgbEqualTo(p0)
						        || !IsValidRankColor(p1)
						        && !IsTeamColor(p1)
						        && !Color.White.IsRgbEqualTo(p1)
						        && !IsValidRankColor(p2)
						        && !IsTeamColor(p2)
						        && !Color.White.IsRgbEqualTo(p2)))
							continue;

						foundValidColor = true;
						break;
					}

					if (!foundValidColor)
						continue;

					var ttlBytes = new StringBuilder();
					var tempX = x;
					var whiteParticleFound = false;
					var redParticleFound = false;
					// gets one character 
					while (ttlBytes.Length == 0 || ttlBytes.ToString().Substring(ttlBytes.Length - 8) != "00000000")
					{
						var columnBytes = new StringBuilder();
						for (var dy = 0; dy < 8 * GuiWidth && tempX < EndingPoint.X; dy += GuiWidth)
						{
							var pixel = Img[tempX, y + dy];
							if (Color.White.IsRgbEqualTo(pixel))
								whiteParticleFound = true;
							// RedTeamColor is the same as the red color
							// (youtuber or admin or watchdog)
							else if (RedTeamColor.IsRgbEqualTo(pixel))
								redParticleFound = true;

							columnBytes.Append(IsValidRankColor(pixel)
							                   || IsTeamColor(pixel)
							                   || Color.White.IsRgbEqualTo(pixel)
								? "1"
								: "0");
						}

						ttlBytes.Append(columnBytes.ToString());
						tempX += GuiWidth;
					}

					ttlBytes = new StringBuilder(ttlBytes.ToString().Substring(0, ttlBytes.Length - 8));

					if (!BinaryToCharactersMap.ContainsKey(ttlBytes.ToString()))
						continue;

					// found first character, search for future characters
					// starting from this x val
					startX = x;

					// empty character comes from
					// the character directly following the 
					// team letter
					if (BinaryToCharactersMap[ttlBytes.ToString()] == string.Empty)
					{
						inGame = true;
						continue;
					}

					// basically, if we're in a game
					// the white names will ALWAYS b e at top of the list
					// if we're in a lobby
					// white names are NEVER possible
					// but red names are. 
					if (redParticleFound && !inGame || whiteParticleFound)
						break;

					realX = x;
					break;
				}


				// end for
				if (realX != -1)
					break;
			}

			if (realX == -1)
				throw new InvalidImageException("Couldn't find any Minecraft characters.");

			IsLobby = !inGame;
			StartingPoint = new Point(realX, y);
		}

		/// <summary>
		/// Parses the names from a screenshot. If the screenshot is a lobby screenshot, then there will only be one key: "Unknown."
		/// </summary>
		/// <param name="exempt">The list of players to not check.</param>
		/// <returns></returns>
		public IDictionary<TeamColor, IList<string>> ParseNames(IList<string> exempt = null)
		{
			exempt ??= new List<string>();
			exempt = exempt
				.Select(x => x.ToLower())
				.ToList();
			var currentColor = TeamColor.Unknown;

			var names = new Dictionary<TeamColor, IList<string>>();
			for (var y = StartingPoint.Y; y <= EndingPoint.Y; y += 9 * GuiWidth)
			{
				var name = new StringBuilder();
				var x = StartingPoint.X;

				while (true)
				{
					var ttlBytes = new StringBuilder();

					while (ttlBytes.Length == 0 || ttlBytes.ToString().Substring(ttlBytes.Length - 8) != "00000000")
					{
						var columnBytes = new StringBuilder();
						for (var dy = 0; dy < 8 * GuiWidth; dy += GuiWidth)
						{
							var color = Img[x, y + dy];
							var isTeamColor = IsTeamColor(color);
							var isRankColor = IsValidRankColor(color);
							if (isTeamColor || isRankColor)
							{
								if (!IsLobby)
									currentColor = GetCurrentColor(color);
								columnBytes.Append("1");
							}
							else
								columnBytes.Append("0");
						}

						ttlBytes.Append(columnBytes.ToString());
						x += GuiWidth;
					}

					ttlBytes = new StringBuilder(ttlBytes.ToString().Substring(0, ttlBytes.Length - 8));

					if (BinaryToCharactersMap.ContainsKey(ttlBytes.ToString()))
						name.Append(BinaryToCharactersMap[ttlBytes.ToString()]);
					else
						break;
				}

				// no name, no go
				if (name.ToString().Trim() == string.Empty)
					continue;

				// exempt names only apply if in lobby
				if (exempt.IndexOf(name.ToString().ToLower()) != -1 && IsLobby)
					continue;

				if (!names.ContainsKey(currentColor))
					names.Add(currentColor, new List<string>());

				names[currentColor].Add(name.ToString());
			}

			return names;
		}

		/// <summary>
		/// Disposes the image.
		/// </summary>
		public void Dispose() => Img?.Dispose();

		/// <summary>
		/// Whether the color specified is a valid color.
		/// </summary>
		/// <param name="color">The color to check.</param>
		/// <returns>Whether the color is valid.</returns>
		private static bool IsValidRankColor(Color color)
			=> MvpPlusPlus.IsRgbEqualTo(color)
			   || MvpPlus.IsRgbEqualTo(color)
			   || Mvp.IsRgbEqualTo(color)
			   || VipPlus.IsRgbEqualTo(color)
			   || Vip.IsRgbEqualTo(color)
			   || None.IsRgbEqualTo(color);

		/// <summary>
		/// Whether the color specified is a team color.
		/// </summary>
		/// <param name="color">The color.</param>
		/// <returns>Whether the color is a team color.</returns>
		private static bool IsTeamColor(Color color)
			=> RedTeamColor.IsRgbEqualTo(color)
			   || BlueTeamColor.IsRgbEqualTo(color)
			   || YellowTeamColor.IsRgbEqualTo(color)
			   || GreenTeamColor.IsRgbEqualTo(color)
			   || AquaTeamColor.IsRgbEqualTo(color)
			   || GreyTeamColor.IsRgbEqualTo(color)
			   || PinkTeamColor.IsRgbEqualTo(color)
			   || WhiteTeamColor.IsRgbEqualTo(color);

		/// <summary>
		/// Gets the current team color.
		/// </summary>
		/// <param name="color">The input color.</param>
		/// <returns>The team color as an enum flag.</returns>
		private static TeamColor GetCurrentColor(Color color) =>
			BlueTeamColor.IsRgbEqualTo(color)
				? TeamColor.Blue
				: RedTeamColor.IsRgbEqualTo(color)
					? TeamColor.Red
					: YellowTeamColor.IsRgbEqualTo(color)
						? TeamColor.Yellow
						: GreenTeamColor.IsRgbEqualTo(color)
							? TeamColor.Green
							: AquaTeamColor.IsRgbEqualTo(color)
								? TeamColor.Aqua
								: GreyTeamColor.IsRgbEqualTo(color)
									? TeamColor.Grey
									: PinkTeamColor.IsRgbEqualTo(color)
										? TeamColor.Pink
										: WhiteTeamColor.IsRgbEqualTo(color)
											? TeamColor.White
											: TeamColor.Unknown;
	}
}