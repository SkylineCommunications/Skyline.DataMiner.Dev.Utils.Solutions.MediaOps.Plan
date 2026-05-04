namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageProperties = Storage.DOM.SlcProperties;

	/// <summary>
	/// Provides a base class for property values associated with a DOM property value section.
	/// </summary>
	public abstract class PropertyValueBase : TrackableObject
	{
		private StorageProperties.PropertyValueSection originalSection;
		private StorageProperties.PropertyValueSection updatedSection;

		private protected PropertyValueBase()
		{
			IsNew = true;
		}

		internal PropertyValueBase(StorageProperties.PropertyValueSection section)
		{
			ParseSection(section);
		}

		/// <summary>
		/// Gets the name of the property value.
		/// </summary>
		public string Name { get; protected set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));
		}
	}
}