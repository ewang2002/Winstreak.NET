using System;

namespace Winstreak.Parsers.ImageParser.ImgExcept
{
	public class InvalidImageException : Exception
	{
		public InvalidImageException(string msg) : base(msg)
		{
		}
	}
}