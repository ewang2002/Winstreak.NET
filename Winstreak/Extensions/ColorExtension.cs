using System.Drawing;

namespace Winstreak.Extension
{
	public static class ColorExtension
	{
		public static bool IsRgbEqualTo(this Color left, Color right)
		{
			return left.R == right.R && left.G == right.G && left.B == right.B; 
		}
	}
}