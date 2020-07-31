using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Winstreak.MethodExtensions
{
	public static class ListExtensions
	{
		public static string ToReadableString<T>(this IList<T> list)
		{
			return $"[{string.Join(", ", list.ToArray())}]";
		}
	}

	public static class ArrayExtensions
	{
		public static string ToReadableString<T>(this T[] list)
		{
			return $"[{string.Join(", ", list.ToArray())}]";
		}
	}
}