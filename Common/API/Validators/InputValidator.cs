namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    internal static class InputValidator
    {
        private const int DefaultMaxTextLength = 150;

        public static bool ValidateEmptyText(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return true;
        }

        public static bool ValidateTextLength(string text)
        {
            return ValidateTextLength(text, DefaultMaxTextLength);
        }

        public static bool ValidateTextLength(string text, int maxCharacters)
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
