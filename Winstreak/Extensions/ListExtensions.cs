using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Winstreak.Extensions
{
	public static class ListExtensions
	{
		public static string ToReadableString<T>(this IList<T> list)
		{
			return string.Join(", ", list.ToArray());
		}
	}
}