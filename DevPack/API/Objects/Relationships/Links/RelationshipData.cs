namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents the data required to create a <see cref="Relationship"/>.
	/// </summary>
	public class RelationshipData
	{
		/// <summary>
		/// Gets or sets the parent side of the relationship.
		/// </summary>
		public RelationshipEndpoint Parent { get; set; }

		/// <summary>
		/// Gets or sets the child side of the relationship.
		/// </summary>
		public RelationshipEndpoint Child { get; set; }

		internal void Validate(string paramName)
		{
			if (Parent == null)
			{
				throw new ArgumentException($"'{nameof(Parent)}' must be filled out.", paramName);
			}

			if (Child == null)
			{
				throw new ArgumentException($"'{nameof(Child)}' must be filled out.", paramName);
			}
		}
	}
}
