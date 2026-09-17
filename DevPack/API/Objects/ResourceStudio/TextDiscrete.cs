namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a discrete value of type string with an associated display name.
	/// </summary>
	public class TextDiscrete : Discrete<string>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TextDiscrete"/> class.
		/// </summary>
		public TextDiscrete()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextDiscrete"/> class with the specified value and display name.
		/// </summary>
		/// <param name="value">Value of the <see cref="TextDiscrete"/>.</param>
		/// <param name="displayName">Display name of the <see cref="TextDiscrete"/>.</param>
		public TextDiscrete(string value, string displayName) : base(value, displayName)
		{
			if (value == null)
				throw new ArgumentException(nameof(value));

			if (displayName == null)
				throw new ArgumentException(nameof(displayName));
		}
	}
}
