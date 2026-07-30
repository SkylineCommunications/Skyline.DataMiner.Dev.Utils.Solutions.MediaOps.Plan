namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Contains the data that can only be provided when a <see cref="Property"/> is created.
	/// </summary>
	public class PropertyData
	{
		/// <summary>
		/// Gets or sets the scope of the property. This is a required field.
		/// </summary>
		public string Scope { get; set; }

		internal void Validate(string paramName)
		{
			if (string.IsNullOrWhiteSpace(Scope))
			{
				throw new ArgumentException($"'{nameof(Scope)}' must be filled out.", paramName);
			}
		}
	}
}
