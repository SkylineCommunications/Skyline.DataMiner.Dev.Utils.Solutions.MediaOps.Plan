namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents the base for all named API objects in the MediaOps Plan API.
	/// </summary>
	public abstract class ApiNamedObject : ApiObject
	{
		private protected ApiNamedObject()
			: base()
		{
		}

		private protected ApiNamedObject(Guid id)
			: base(id)
		{
		}

		/// <summary>
		/// Gets or sets the name of the API object.
		/// </summary>
		public abstract string Name { get; set; }
	}
}
