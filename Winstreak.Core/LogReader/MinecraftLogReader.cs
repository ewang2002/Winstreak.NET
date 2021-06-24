using System;
using System.IO;
using System.Text;
using System.Timers;

namespace Winstreak.Core.LogReader
{
	/// <summary>
	/// The MinecraftLogReader is designed to provide an easy way to continuously read the Minecraft log file.
	/// </summary>
	public class MinecraftLogReader : IDisposable
	{
		public bool IsStarted { get; private set; }
		private bool _disposed;

		private readonly FileStream _stream;
		private readonly Timer _timer;

		public EventHandler<string> OnLogUpdate;

		/// <summary>
		/// Creates a new MinecraftLogReader instance. This is intended to provide an easy way to continuously read the
		/// Minecraft log file.
		/// </summary>
		/// <param name="pathToLogs">The path to the logs folder.</param>
		/// <exception cref="DirectoryNotFoundException">If the Minecraft folder is invalid.</exception>
		public MinecraftLogReader(string pathToLogs)
		{
			if (!Directory.Exists(pathToLogs))
				throw new DirectoryNotFoundException("Log folder not found.");

			if (!File.Exists(Path.Join(pathToLogs, "latest.log")))
				File.Create(Path.Join(pathToLogs, "latest.log"));

			IsStarted = false;
			_disposed = false;

			var logFile = Path.Join(pathToLogs, "latest.log");

			// Start a new stream and put it at the end.
			_stream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			_stream.Seek(0, SeekOrigin.End);

			// Check the log file every 100 ms.
			_timer = new Timer
			{
				Interval = 100,
				AutoReset = true,
				Enabled = false
			};

			_timer.Elapsed += (_, __) => CheckForUpdates();
		}

		/// <summary>
		/// The destructor for this object.
		/// </summary>
		~MinecraftLogReader() => Dispose(false);

		/// <summary>
		/// Starts the MinecraftLogReader.
		/// </summary>
		public void Start()
		{
			if (IsStarted) return;
			IsStarted = true;
			_timer.Start();
		}

		/// <summary>
		/// Stops the MinecraftLogReader.
		/// </summary>
		public void Stop()
		{
			if (!IsStarted) return;
			IsStarted = false;
			_timer.Stop();
		}

		/// <summary>
		/// Disposes this MinecraftLogReader.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed) return;
			Stop();
			_stream.Dispose();
			_timer.Dispose();
			_disposed = true;
		}

#nullable enable
		// https://stackoverflow.com/questions/6740679/capturing-standard-out-from-tail-f-follow.
		private void CheckForUpdates(Encoding? encoding = null)
		{
			encoding ??= Encoding.Default;
			var tail = new StringBuilder();
			int read;
			var bytes = new byte[1024];
			while ((read = _stream.Read(bytes, 0, bytes.Length)) > 0)
				tail.Append(encoding.GetString(bytes, 0, read));

			if (tail.Length > 0) OnLogUpdate.Invoke(null, tail.ToString().Trim());
			else _stream.Seek(0, SeekOrigin.End);
		}
#nullable disable
	}
}