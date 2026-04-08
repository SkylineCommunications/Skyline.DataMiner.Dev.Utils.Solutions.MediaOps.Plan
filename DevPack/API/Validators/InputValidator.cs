namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	internal static class InputValidator
	{
		internal const int DefaultMaxTextLength = 150;

		public static bool IsNonEmptyText(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}

			return true;
		}

		public static bool HasValidTextLength(string text, int maxCharacters = DefaultMaxTextLength)
		{
			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			if (text.Length > maxCharacters)
			{
				return false;
			}

			return true;
		}
	}
}
