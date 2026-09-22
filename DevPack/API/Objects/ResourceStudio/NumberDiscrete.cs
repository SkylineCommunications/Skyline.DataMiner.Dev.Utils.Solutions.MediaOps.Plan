namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a discrete numeric value with an associated display name, using a decimal type for precision.
	/// </summary>
	public class NumberDiscrete : Discrete<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NumberDiscrete"/> class.
		/// </summary>
		public NumberDiscrete()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NumberDiscrete"/> class with the specified value and display name.
		/// </summary>
		/// <param name="value">Value of the <see cref="NumberDiscrete"/>.</param>
		/// <param name="displayName">Display name of the <see cref="NumberDiscrete"/>.</param>
		public NumberDiscrete(decimal value, string displayName) : base(value, displayName)
		{
			if (displayName == null)
				throw new ArgumentNullException(nameof(displayName));
		}
	}
}
