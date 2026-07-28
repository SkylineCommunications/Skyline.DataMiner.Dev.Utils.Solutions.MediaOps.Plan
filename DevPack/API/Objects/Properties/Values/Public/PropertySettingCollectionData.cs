namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Input data used to initialize a new <see cref="PropertySettingCollection"/>.
	/// </summary>
	public sealed class PropertySettingCollectionData
	{
		private string scope;

		/// <summary>
		/// Gets or sets the optional identifier of the object this collection is linked to.
		/// </summary>
		public string LinkedObjectId { get; set; }

		/// <summary>
		/// Gets or sets the scope of this property setting collection. This value is required.
		/// </summary>
		public string Scope
		{
			get => scope;
			set
			{
				if (string.IsNullOrWhiteSpace(value))
				{
					throw new ArgumentException("Scope cannot be null or whitespace.", nameof(value));
				}

				scope = value;
			}
		}

		/// <summary>
		/// Gets or sets the optional sub-identifier for this property setting collection.
		/// </summary>
		public string SubId { get; set; }
	}
}
