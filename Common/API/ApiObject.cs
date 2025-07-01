namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;


	/// <summary>
	/// Represents the base class for all API objects in the MediaOps Plan API.
	/// </summary>
    public abstract class ApiObject
	{
		/// <summary>
		/// Gets the unique identifier of the API object.
		/// </summary>
		public Guid Id { get; private set; }

		internal ApiObject()
			: this(Guid.NewGuid())
		{
		}

		internal ApiObject(Guid id)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentNullException(nameof(id));
			}

			Id = id;
		}

		internal abstract bool IsNew { get; set; }

		internal abstract bool HasUserDefinedId { get; set; }

		internal abstract bool HasChanges { get; set; }
	}
}
