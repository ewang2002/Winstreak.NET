using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Winstreak.Extensions;
using static Winstreak.Parser.Constants;

namespace Winstreak.Parser.V1
{
	public class LobbyNameParser : AbstractNameParser
	{
		public LobbyNameParser(Bitmap img) : base(img)
		{
		}

		public LobbyNameParser(string path) : base(path)
		{
		}


		public override (IList<string> lobby, IDictionary<TeamColors, IList<string>> team) GetPlayerName(
			IList<string> exempt = null)
		{
			exempt ??= new List<string>();

			IList<string> names = new List<string>();
			for (int y = StartingPoint.Y; y <= EndingPoint.Y; y += 9 * GuiWidth)
			{
				StringBuilder name = new StringBuilder();
				int x = StartingPoint.X;

				while (true)
				{
					StringBuilder ttlBytes = new StringBuilder();

					while (ttlBytes.Length == 0 || ttlBytes.ToString().Substring(ttlBytes.Length - 8) != "00000000")
					{
						StringBuilder columnBytes = new StringBuilder();
						for (int dy = 0; dy < 8 * base.GuiWidth; dy += base.GuiWidth)
							columnBytes.Append(IsValidColor(base.Img.GetPixel(x, y + dy)) ? "1" : "0");

						ttlBytes.Append(columnBytes.ToString());
						x += base.GuiWidth;
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