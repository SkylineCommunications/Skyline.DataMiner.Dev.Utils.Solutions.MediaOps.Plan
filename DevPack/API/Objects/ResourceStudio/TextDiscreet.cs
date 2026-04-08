namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a discreet value of type string with an associated display name.
	/// </summary>
	public class TextDiscreet : Discreet<string>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TextDiscreet"/> class.
		/// </summary>
		public TextDiscreet()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextDiscreet"/> class with the specified value and display name.
		/// </summary>
		/// <param name="value">Value of the <see cref="TextDiscreet"/>.</param>
		/// <param name="displayName">Display name of the <see cref="TextDiscreet"/>.</param>
		public TextDiscreet(string value, string displayName) : base(value, displayName)
		{
			if (value == null)
				throw new ArgumentException(nameof(value));

			if (displayName == null)
				throw new ArgumentException(nameof(displayName));
		}
	}
}
