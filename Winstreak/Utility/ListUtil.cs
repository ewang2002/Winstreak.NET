using System;
using System.Collections.Generic;
using System.Linq;

namespace Winstreak.Utility
{
	public static class ListUtil
	{
		/// <summary>
		/// Groups a list of sublists based on common elements shared. This method should be executed twice -- the first time for initial sorting, and the second time to ensure that all elements are sorted.
		/// </summary>
		/// <param name="bigArr">The 2D list to sort.</param>
		/// <returns>A 2D list where each list has distinct elements.</returns>
		public static IList<IList<string>> GetGroups(IList<IList<string>> bigArr)
		{
			var arr = new List<IList<string>>();
			foreach (var element in bigArr)
			{
				var index = arr.FindIndex(x => element.Any(x.Contains));
				if (index == -1)
					arr.Add(element.Distinct().ToList());
				else
					arr[index] = element.Distinct().ToList();
			}

			return arr; 
		}
	}
}