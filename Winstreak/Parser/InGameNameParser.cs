using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Winstreak.Extensions;
using static Winstreak.Parser.Constants;

namespace Winstreak.Parser
{
	public class InGameNameParser : AbstractNameParser
	{
		/// <inheritdoc />
		public InGameNameParser(Bitmap image) : base(image)
		{
		}

		/// <inheritdoc />
		public InGameNameParser(string file) : base(file)
		{
		}
		
		/// <summary>
		/// Accounts for the team letters (R, G, Y, B) by skipping the team letters altogether and going to the first name. 
		/// </summary>
		public void AccountForTeamLetters() =>
			StartingPoint = new Point(StartingPoint.X + 12 * GuiWidth, StartingPoint.Y);

		/// <inheritdoc />
		public override (IList<string> lobby, IDictionary<TeamColors, IList<string>> team) GetPlayerName(
			IList<string> exempt = null)
		{
			exempt ??= new List<string>();

			var teammates = new Dictionary<TeamColors, IList<string>>();
			var colorsToIgnore = new List<TeamColors>();

			var currentColor = TeamColors.Unknown;
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
						for (var dy = 0; dy < 8 * base.GuiWidth; dy += base.GuiWidth)
						{
							var color = base.Img.GetPixel(x, y + dy);
							if (IsValidColor(color))
							{
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

				if (exempt.Contains(name.ToString()))
					colorsToIgnore.Add(currentColor);

				if (colorsToIgnore.Contains(currentColor) || name.ToString().Trim().Equals(string.Empty)) 
					continue;

				if (currentColor == TeamColors.Unknown)
					continue;

				if (!teammates.ContainsKey(currentColor))
					teammates.Add(currentColor, new List<string>());

				teammates[currentColor].Add(name.ToString());
			}

			return (new List<string>(), teammates);
		}

		/// <summary>
		/// Gets the current team color.
		/// </summary>
		/// <param name="color">The input color.</param>
		/// <returns>The team color as an enum flag.</returns>
		private TeamColors GetCurrentColor(Color color)
		{
			return BlueTeamColor.IsRgbEqualTo(color)
				? TeamColors.Blue
				: RedTeamColor.IsRgbEqualTo(color)
					? TeamColors.Red
					: YellowTeamColor.IsRgbEqualTo(color)
						? TeamColors.Yellow
						: GreenTeamColor.IsRgbEqualTo(color)
							? TeamColors.Green
							: TeamColors.Unknown;
		}

		/// <inheritdoc />
		public override bool IsValidColor(Color color)
		{
			return RedTeamColor.IsRgbEqualTo(color)
			       || BlueTeamColor.IsRgbEqualTo(color)
			       || YellowTeamColor.IsRgbEqualTo(color)
			       || GreenTeamColor.IsRgbEqualTo(color);
		}
	}
}