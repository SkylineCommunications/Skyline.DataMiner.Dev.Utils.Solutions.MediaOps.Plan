namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents the base for all named API objects in the MediaOps Plan API.
	/// </summary>
	/// <seealso cref="Job"/>
	/// <seealso cref="Parameter"/>
	/// <seealso cref="Property"/>
	/// <seealso cref="RecurringJob"/>
	/// <seealso cref="Resource"/>
	/// <seealso cref="ResourcePool"/>
	/// <seealso cref="ResourceProperty"/>
	/// <seealso cref="Workflow"/>
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
