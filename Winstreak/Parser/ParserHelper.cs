using System.IO;

namespace Winstreak.Parser
{
	public abstract class ParserHelper
	{
		/// <summary>
		/// Gets the defined Gui scale from your Minecraft settings. 
		/// </summary>
		/// <param name="pathToMcFolder">The path to your Minecraft folder of interest,</param>
		/// <returns>The Gui scale.</returns>
		public static int GetGuiScale(string pathToMcFolder)
		{
			string realPath = Path.Join(pathToMcFolder, "options.txt");
			string[] options = File.ReadAllLines(realPath);

			for (int i = 0; i < options.Length; i++)
			{
				if (options[i].StartsWith("guiScale"))
				{
					return int.Parse(options[i].Split(":")[1]);
				}
			}

			return -1; 
		}
	}
}