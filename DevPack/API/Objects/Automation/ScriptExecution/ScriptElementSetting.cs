namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	/// <summary>
	/// Represents a script dummy that can be assigned to an automation script.
	/// </summary>
	public class ScriptElementSetting
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptElementSetting"/> class.
		/// </summary>
		/// <param name="name">The name of the ScriptDummy. Cannot be null or empty.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is null or empty.</exception>
		public ScriptElementSetting(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException(nameof(name));
			}

			Name = name;
		}

		/// <summary>
		/// Gets the name of the script dummy.
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		/// Gets or sets the unique identifier for the associated DMS element.
		/// </summary>
		public DmsElementId DmsElementId { get; set; }

		/// <summary>
		/// Gets or sets the name of the element.
		/// </summary>
		public string ElementName { get; set; }

		/// <summary>
		/// Gets or sets a reference to a data source that provides the element ID.
		/// </summary>
		public DataReference Reference { get; set; }

		/// <summary>
		/// Gets a value indicating whether this setting has a reference defined.
		/// </summary>
		public bool HasReference => Reference != null;
	}
}
