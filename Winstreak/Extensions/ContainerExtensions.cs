using System.Linq;

namespace Winstreak.Extensions
{
	public static class ArrayExtensions
	{
		/// <summary>
		/// Essentially the ToString method, but shows all elements. 
		/// </summary>
		/// <typeparam name="T">The object type.</typeparam>
		/// <param name="list">The array of objects.</param>
		/// <returns>All the string-represented elements in the array.</returns>
		public static string ToReadableString<T>(this T[] list)
			=> $"[{string.Join(", ", list.ToArray())}]";
	}
}