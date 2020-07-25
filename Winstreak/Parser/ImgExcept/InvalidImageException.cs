using System;

namespace Winstreak.Parser.ImgExcept
{
	public class InvalidImageException : Exception
	{
		public InvalidImageException(string msg) : base(msg)
		{
		}
	}
}