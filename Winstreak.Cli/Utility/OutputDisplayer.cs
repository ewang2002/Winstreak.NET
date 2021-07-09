using System;
using System.Text;

namespace Winstreak.Cli.Utility
{
	public static class OutputDisplayer
	{
		/// <summary>
		/// Writes to the Console. This will also include a correctly formatted timestamp.
		/// </summary>
		/// <typeparam name="T">The content type..</typeparam>
		/// <param name="lt">The log type.</param>
		/// <param name="content">The content to print out.</param>
		public static void WriteLine<T>(LogType lt, T content)
		{
			var sb = new StringBuilder()
				.Append($"[{DateTime.Now:HH:mm:ss}] ");

			switch (lt)
			{
				case LogType.Info:
					sb.Append("[Info] ");
					break;
				case LogType.Error:
					Console.ForegroundColor = ConsoleColor.DarkRed;
					sb.Append("[Error] ");
					break;
				case LogType.Warning:
					Console.ForegroundColor = ConsoleColor.DarkYellow;
					sb.Append("[Warn] ");
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(lt), lt, null);
			}

			sb.Append(content);
			Console.WriteLine(sb.ToString());
			Console.ResetColor();
		}
	}

	public enum LogType
	{
		Info,
		Error,
		Warning
	}
}