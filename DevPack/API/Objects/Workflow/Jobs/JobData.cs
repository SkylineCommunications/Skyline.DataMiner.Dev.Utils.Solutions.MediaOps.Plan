namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Contains the data that can only be provided when a <see cref="Job"/> is created.
	/// </summary>
	public class JobData
	{
		/// <summary>
		/// Gets or sets the key of the job. This is a required field. When no key should be provided, use one of the constructors that does not take a <see cref="JobData"/> instance so that the system assigns a generated key.
		/// </summary>
		public string Key { get; set; }

		internal void Validate(string paramName)
		{
			if (string.IsNullOrWhiteSpace(Key))
			{
				throw new ArgumentException($"'{nameof(Key)}' must be filled out.", paramName);
			}
		}
	}
}
