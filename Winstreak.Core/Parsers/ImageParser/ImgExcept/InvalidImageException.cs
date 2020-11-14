using System;

namespace Winstreak.Core.Parsers.ImageParser.ImgExcept
{
	public class InvalidImageException : Exception
	{
		public InvalidImageException(string msg) : base(msg)
		{
		}
	}
}