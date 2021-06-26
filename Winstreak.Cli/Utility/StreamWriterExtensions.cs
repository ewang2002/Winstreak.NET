using System;
using System.IO;
using System.Threading.Tasks;

namespace Winstreak.Cli.Utility
{
	public static class StreamWriterExtensions
	{
		public static async Task LogWriteLineAsync(this StreamWriter writer, string content)
			=> await writer.WriteLineAsync($"[{DateTime.Now:HH:mm:ss}] {content}");
	}
}