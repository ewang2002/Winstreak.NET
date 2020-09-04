using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Winstreak.Extensions;
using static Winstreak.Parser.Constants;

namespace Winstreak.Parser
{
	public class LobbyNameParser : AbstractNameParser
	{
		/// <inheritdoc />
		public LobbyNameParser(Bitmap img) : base(img)
		{
		}

		/// <inheritdoc />
		public LobbyNameParser(string path) : base(path)
		{
		}

		/// <inheritdoc />
		public override (IList<string> lobby, IDictionary<TeamColors, IList<string>> team) GetPlayerName(
			IList<string> exempt = null)
		{
			exempt ??= new List<string>();

			var names = new List<string>();
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
							columnBytes.Append(IsValidColor(base.Img.GetPixel(x, y + dy)) ? "1" : "0");

						ttlBytes.Append(columnBytes.ToString());
						x += GuiWidth;
					}

					ttlBytes = new StringBuilder(ttlBytes.ToString().Substring(0, ttlBytes.Length - 8));
					//ttlBytes = new StringBuilder(ttlBytes.ToString()[0..^8]);


					if (BinaryToCharactersMap.ContainsKey(ttlBytes.ToString()))
						name.Append(BinaryToCharactersMap[ttlBytes.ToString()]);
					else
						break;
				}

				if (!exempt.Contains(name.ToString()))
					names.Add(name.ToString());
			}

			names = names
				.Where(x => x.Length != 0)
				.ToList();

			return (names, new Dictionary<TeamColors, IList<string>>());
		}

		/// <inheritdoc />
		public override bool IsValidColor(Color c)
		{
			return MvpPlusPlus.IsRgbEqualTo(c)
			       || MvpPlus.IsRgbEqualTo(c)
			       || Mvp.IsRgbEqualTo(c)
			       || VipPlus.IsRgbEqualTo(c)
			       || Vip.IsRgbEqualTo(c)
			       || None.IsRgbEqualTo(c);
		}
	}
}