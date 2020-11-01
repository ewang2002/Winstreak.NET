using System.IO;

namespace Winstreak.Parsers.ImageParser
{
	public static class ParserHelper
	{
		/// <summary>
		/// Gets the defined Gui scale from your Minecraft settings. 
		/// </summary>
		/// <param name="pathToMcFolder">The path to your Minecraft folder of interest,</param>
		/// <returns>The Gui scale.</returns>
		public static int GetGuiScale(string pathToMcFolder)
		{
			var realPath = Path.Join(pathToMcFolder, "options.txt");
			var options = File.ReadAllLines(realPath);

			foreach (var option in options)
			{
				if (option.StartsWith("guiScale"))
					return int.Parse(option.Split(":")[1]);
			}

			return -1;
		}
	}
}