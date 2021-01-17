using System;
using System.Collections.Generic;

namespace Winstreak.Core.Parsers.ImageParser
{
	/// <summary>
	/// All classes that implement this interface should be used for the purpose of processing Minecraft names. 
	/// </summary>
	public interface INameParser : IDisposable
	{
		/// <summary>
		/// Parses the names from a screenshot. If the screenshot is a lobby screenshot, then there will only be one key: "Unknown."
		/// </summary>
		/// <param name="exempt">The list of players to not check.</param>
		/// <returns>The parsed names.</returns>
		public IDictionary<TeamColor, IList<string>> ParseNames(IList<string> exempt = null);
		
		/// <summary>
		/// Whether the screenshot represents a lobby screenshot. 
		/// </summary>
		public bool IsLobby { get; }
	}
}