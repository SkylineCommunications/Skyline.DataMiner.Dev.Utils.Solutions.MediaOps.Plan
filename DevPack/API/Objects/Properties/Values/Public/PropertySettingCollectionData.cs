namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Contains the data that can only be provided when a <see cref="PropertySettingCollection"/> is created.
	/// </summary>
	public class PropertySettingCollectionData
	{
		/// <summary>
		/// Gets or sets the identifier of the object the collection is linked to. This is a required field.
		/// </summary>
		public string LinkedObjectId { get; set; }

		/// <summary>
		/// Gets or sets the scope of the collection. This is a required field.
		/// </summary>
		public string Scope { get; set; }

		/// <summary>
		/// Gets or sets the sub-identifier of the collection. This is a required field, but it may be empty when the collection is linked to the object itself instead of to one of its sub-objects.
		/// </summary>
		public string SubId { get; set; }

		internal void Validate(string paramName)
		{
			if (string.IsNullOrWhiteSpace(LinkedObjectId))
			{
				throw new ArgumentException($"'{nameof(LinkedObjectId)}' must be filled out.", paramName);
			}

			if (string.IsNullOrWhiteSpace(Scope))
			{
				throw new ArgumentException($"'{nameof(Scope)}' must be filled out.", paramName);
			}

			if (SubId == null)
			{
				throw new ArgumentException($"'{nameof(SubId)}' must be filled out.", paramName);
			}
		}
	}
}
