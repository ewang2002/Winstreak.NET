using System.Drawing;

namespace Winstreak.Extensions
{
	public static class ColorExtension
	{
		/// <summary>
		/// Determines whether the RGB values of two colors are the same. This is different from directly comparing two colors as this method only considers the RGB values, not the ARGB. 
		/// </summary>
		/// <param name="left">The first color to compare.</param>
		/// <param name="right">The second color to compare with.</param>
		/// <returns>Whether the two colors are the same in terms of RGB.</returns>
		public static bool IsRgbEqualTo(this Color left, Color right)
		{
			return left.R == right.R && left.G == right.G && left.B == right.B;
		}
	}
}