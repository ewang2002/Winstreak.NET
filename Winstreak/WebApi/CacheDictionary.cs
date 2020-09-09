using System;
using System.Collections.Concurrent;
using System.Text;
using System.Timers;

namespace Winstreak.WebApi
{
	/// <summary>
	/// A class that is designed to hold items for a certain period of time. 
	/// </summary>
	public class CacheDictionary<TK, TV>
	{
		private readonly ConcurrentDictionary<TK, (TV val, Timer timer)> _dict;

		/// <summary>
		/// Creates a new CacheDictionary
		/// </summary>
		public CacheDictionary()
			=> _dict = new ConcurrentDictionary<TK, (TV val, Timer timer)>();

		/// <summary>
		/// Adds a key and a value to the CacheDictionary object. 
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="val">The value.</param>
		/// <param name="removeIn">How long the key/value pair should last before being removed.</param>
		/// <returns>Whether the key was added.</returns>
		public bool TryAdd(TK key, TV val, TimeSpan removeIn)
		{
			if (_dict.ContainsKey(key))
				return false;

			var timer = new Timer
			{
				AutoReset = false,
				Interval = removeIn.TotalMilliseconds,
				Enabled = true
			};
			_dict.TryAdd(key, (val, timer));

			timer.Elapsed += (sender, args) =>
			{
				timer.Stop();
				_dict.TryRemove(key, out _);
			};

			return true;
		}

		/// <summary>
		/// Adds a key and a value to the CacheDictionary object, with a default removal time of 5 minutes.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="val">The value.</param>
		/// <returns>Whether the key was added.</returns>
		public bool TryAdd(TK key, TV val) => TryAdd(key, val, TimeSpan.FromMinutes(5));

		/// <summary>
		/// For a key/value pair, resets the cache removal time to a specified time.
		/// </summary>
		/// <param name="key">The key,</param>
		/// <param name="newTime">The new time. This will overwrite the previous time.</param>
		/// <returns>Whether the key/value pair existed.</returns>
		public bool ResetCacheTime(TK key, TimeSpan newTime)
		{
			if (!_dict.ContainsKey(key))
				return false;

			_dict[key].timer.Stop();
			_dict[key].timer.Interval = newTime.TotalMilliseconds;
			_dict[key].timer.Start();
			return true;
		}

		/// <summary>
		/// For a key/value pair, resets the cache removal time to the default time of 30 minutes.
		/// </summary>
		/// <param name="key">The key,</param>
		/// <returns>Whether the key/value pair existed.</returns>
		public bool ResetCacheTime(TK key) => ResetCacheTime(key, TimeSpan.FromMinutes(5));

		/// <summary>
		/// Removes a key and value pair from the CacheDictionary.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>Whether the removal was successful.</returns>
		public bool TryRemove(TK key)
		{
			if (!_dict.ContainsKey(key))
				return false;

			_dict.TryRemove(key, out var value);
			value.timer.Stop();
			return true;
		}

		/// <summary>
		/// Returns the string representation of this object.
		/// </summary>
		/// <returns>The string representation of this object.</returns>
		public override string ToString()
		{
			var b = new StringBuilder();
			foreach (var (key, val) in _dict)
			{
				b.Append($"{key} => {val.val}")
					.AppendLine();
			}

			return b.ToString();
		}

		/// <summary>
		/// Returns the value associated with the key.
		/// </summary>
		/// <param name="key">The key,</param>
		/// <returns>The value.</returns>
		public TV this[TK key] => _dict[key].val;

		/// <summary>
		/// Whether the CacheDictionary contains the specified key.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns>Whether the key exists.</returns>
		public bool Contains(TK key) => _dict.ContainsKey(key);
	}
}